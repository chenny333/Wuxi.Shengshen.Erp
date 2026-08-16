import { createHash, randomUUID } from "node:crypto";
import {
  lstat,
  mkdir,
  readFile,
  stat,
  symlink,
  writeFile
} from "node:fs/promises";
import path from "node:path";
import { normalizePath } from "./config.js";
import { runProcess } from "./process-runner.js";

const REVIEW_START = "PIPELINE_REVIEW_RESULT_START";
const REVIEW_END = "PIPELINE_REVIEW_RESULT_END";
const MAX_REWORK_ROUNDS = 3;
const TASK_ID_PATTERN = /^[A-Za-z0-9-]+$/;

/**
 * Runs the isolated development pipeline.
 *
 * @param {Record<string, any>} config pipeline configuration
 * @param {{
 *   task?: string,
 *   repoRoot?: string,
 *   baseRef?: string,
 *   maxReworkRounds?: number,
 *   resumeTaskId?: string,
 *   reviewOutcome?: "approved" | "rework",
 *   reworkFeedback?: string
 * }} input tool input
 * @returns {Promise<Record<string, any>>} structured acceptance report
 */
export async function runDevelopmentPipeline(config, input) {
  const reviewMode = config.reviewMode ?? "agent";
  await ensureJunctions(config);
  if (
    input.resumeTaskId !== undefined &&
    input.resumeTaskId !== null &&
    input.resumeTaskId !== ""
  ) {
    return await resumePipeline(config, input, reviewMode);
  }
  if (typeof input.task !== "string" || input.task.trim() === "") {
    throw new Error("新建流水线必须提供 task");
  }
  return await startPipeline(config, input, reviewMode);
}

/**
 * Starts a fresh pipeline run: creates a worktree and executes rounds.
 */
async function startPipeline(config, input, reviewMode) {
  const startedAt = new Date();
  const repoRoot = path.resolve(input.repoRoot ?? config.allowedRepositories[0]);
  ensureAllowedRepository(config, repoRoot);
  const maxReworkRounds = clampReworkRounds(input.maxReworkRounds);
  const taskId = createTaskId(input.task);
  const branch = `${config.branchPrefix}${taskId}`;
  const worktreePath = path.join(config.worktreeRoot, "worktrees", taskId);
  const runRoot = path.join(config.worktreeRoot, "runs", taskId);
  const report = createReport({
    taskId,
    task: input.task,
    repoRoot,
    baseRef: input.baseRef ?? "HEAD",
    branch,
    worktreePath,
    runRoot,
    reviewMode,
    maxReworkRounds,
    startedAt
  });

  await mkdir(runRoot, { recursive: true });
  try {
    await validateRepository(config, repoRoot, report.baseRef, runRoot);
    await createWorktree(repoRoot, worktreePath, branch, report.baseRef, runRoot);
    await writePipelineState(runRoot, {
      taskId,
      task: input.task,
      repoRoot,
      baseRef: report.baseRef,
      branch,
      reviewMode,
      createdAt: startedAt.toISOString()
    });
    await executeRounds({
      config,
      reviewMode,
      task: input.task,
      feedback: "",
      startRound: 0,
      maxReworkRounds,
      worktreePath,
      runRoot,
      taskId,
      report
    });
  } catch (error) {
    report.status = "failed";
    report.message = error instanceof Error ? error.message : String(error);
  }
  return await finalizeReport(report, startedAt);
}

/**
 * Resumes an existing external-review task on its original worktree.
 * reviewOutcome=approved finalizes the report as success;
 * reviewOutcome=rework runs another developer round with the feedback.
 */
async function resumePipeline(config, input, reviewMode) {
  const startedAt = new Date();
  const taskId = input.resumeTaskId;
  if (!TASK_ID_PATTERN.test(taskId)) {
    throw new Error(`resumeTaskId 格式不合法: ${taskId}`);
  }
  const outcome = input.reviewOutcome;
  if (outcome !== "approved" && outcome !== "rework") {
    throw new Error('续跑必须提供 reviewOutcome（"approved" 或 "rework"）');
  }
  if (
    outcome === "rework" &&
    (typeof input.reworkFeedback !== "string" ||
      input.reworkFeedback.trim() === "")
  ) {
    throw new Error("reviewOutcome=rework 时必须提供非空 reworkFeedback");
  }

  const runRoot = path.join(config.worktreeRoot, "runs", taskId);
  const state = await readPipelineState(runRoot);
  const repoRoot = path.resolve(state.repoRoot);
  ensureAllowedRepository(config, repoRoot);
  const worktreePath = path.join(config.worktreeRoot, "worktrees", taskId);
  if (!(await directoryExists(worktreePath))) {
    throw new Error(`任务 worktree 不存在，无法续跑: ${worktreePath}`);
  }

  const report =
    (await readPreviousReport(runRoot)) ??
    createReport({
      taskId,
      task: state.task,
      repoRoot,
      baseRef: state.baseRef ?? "HEAD",
      branch: state.branch ?? `${config.branchPrefix}${taskId}`,
      worktreePath,
      runRoot,
      reviewMode,
      maxReworkRounds: clampReworkRounds(input.maxReworkRounds),
      startedAt
    });
  report.externalReviews.push({
    outcome,
    feedback: input.reworkFeedback ?? "",
    at: startedAt.toISOString()
  });

  try {
    if (outcome === "approved") {
      report.status = "success";
      report.message = "外部验收通过";
      report.finalFindings = [];
    } else {
      const maxReworkRounds = clampReworkRounds(input.maxReworkRounds ?? 1);
      await executeRounds({
        config,
        reviewMode,
        task: state.task,
        feedback: input.reworkFeedback,
        startRound: report.rounds.length,
        maxReworkRounds,
        worktreePath,
        runRoot,
        taskId,
        report
      });
    }
  } catch (error) {
    report.status = "failed";
    report.message = error instanceof Error ? error.message : String(error);
  }
  return await finalizeReport(report, startedAt);
}

/**
 * Runs developer + verification rounds; in agent reviewMode also runs the
 * read-only reviewer. Mutates report with the outcome and returns nothing.
 */
async function executeRounds(options) {
  const {
    config,
    reviewMode,
    task,
    startRound,
    maxReworkRounds,
    worktreePath,
    runRoot,
    taskId,
    report
  } = options;
  let feedback = options.feedback;
  for (let round = startRound; round <= startRound + maxReworkRounds; round += 1) {
    const developerResult = await runDeveloper({
      config,
      task,
      feedback,
      round,
      worktreePath,
      runRoot,
      taskId
    });
    report.developerRuns.push(summarizeProcess(developerResult));

    const roundReport = {
      round,
      developerSucceeded: isSuccessful(developerResult),
      verification: [],
      review: null,
      blockingIssues: []
    };
    if (!isSuccessful(developerResult)) {
      roundReport.blockingIssues.push({
        source: "developer",
        message: compactFailure(developerResult)
      });
    } else {
      roundReport.verification = await runVerification(
        config,
        worktreePath,
        runRoot,
        round
      );
      const failedVerification = roundReport.verification.filter(
        (item) => !item.succeeded
      );
      failedVerification.forEach((item) => {
        roundReport.blockingIssues.push({
          source: "verification",
          step: item.name,
          message: item.failureSummary,
          logPath: item.logPath
        });
      });

      if (failedVerification.length === 0 && reviewMode === "agent") {
        roundReport.review = await runReviewer({
          config,
          task,
          worktreePath,
          runRoot,
          round,
          taskId
        });
        roundReport.blockingIssues.push(...roundReport.review.blockingIssues);
      }
    }

    report.rounds.push(roundReport);
    if (roundReport.blockingIssues.length === 0) {
      if (reviewMode === "external") {
        report.status = "awaiting_review";
        report.message =
          "开发与编译测试已通过，等待外部验收：用 resumeTaskId + reviewOutcome（approved 结束 / rework 返工）续跑本任务";
      } else {
        report.status = "success";
        report.message = "开发、编译测试和只读审查均已通过";
      }
      return;
    }
    if (round === startRound + maxReworkRounds) {
      report.status = "rejected";
      report.message = `达到最多 ${maxReworkRounds} 轮返工，仍有阻断问题`;
      report.finalFindings = roundReport.blockingIssues;
      return;
    }
    feedback = buildReworkFeedback(roundReport);
  }
}

/**
 * Collects the final diff, stamps timing and persists the report.
 */
async function finalizeReport(report, callStartedAt) {
  if (await directoryExists(report.worktreePath)) {
    report.changedFiles = await readGitLines(
      report.worktreePath,
      ["status", "--short"],
      path.join(report.runRoot, "final-status.log")
    );
    report.diffStat = (
      await runGit(
        report.worktreePath,
        ["diff", "--stat", report.baseRef],
        path.join(report.runRoot, "final-diff-stat.log")
      )
    ).stdout.trim();
  }
  const finishedAt = new Date();
  report.finishedAt = finishedAt.toISOString();
  report.durationMs = finishedAt.getTime() - callStartedAt.getTime();
  await writeFile(
    path.join(report.runRoot, "acceptance-report.json"),
    JSON.stringify(report, null, 2),
    "utf8"
  );
  return report;
}

function createReport(options) {
  return {
    status: "failed",
    taskId: options.taskId,
    task: options.task,
    repoRoot: options.repoRoot,
    baseRef: options.baseRef,
    branch: options.branch,
    worktreePath: options.worktreePath,
    runRoot: options.runRoot,
    reviewMode: options.reviewMode,
    maxReworkRounds: options.maxReworkRounds,
    developerRuns: [],
    rounds: [],
    externalReviews: [],
    finalFindings: [],
    changedFiles: [],
    diffStat: "",
    startedAt: options.startedAt.toISOString(),
    finishedAt: null,
    durationMs: null,
    message: ""
  };
}

async function writePipelineState(runRoot, state) {
  await writeFile(
    path.join(runRoot, "pipeline-state.json"),
    JSON.stringify(state, null, 2),
    "utf8"
  );
}

async function readPipelineState(runRoot) {
  let raw;
  try {
    raw = await readFile(path.join(runRoot, "pipeline-state.json"), "utf8");
  } catch {
    throw new Error(`找不到任务状态文件（pipeline-state.json）: ${runRoot}`);
  }
  const state = JSON.parse(raw);
  if (typeof state.task !== "string" || state.task.trim() === "") {
    throw new Error("任务状态文件缺少 task 字段");
  }
  if (typeof state.repoRoot !== "string" || state.repoRoot.trim() === "") {
    throw new Error("任务状态文件缺少 repoRoot 字段");
  }
  return state;
}

async function readPreviousReport(runRoot) {
  try {
    const report = JSON.parse(
      await readFile(path.join(runRoot, "acceptance-report.json"), "utf8")
    );
    report.developerRuns = report.developerRuns ?? [];
    report.rounds = report.rounds ?? [];
    report.externalReviews = report.externalReviews ?? [];
    report.finalFindings = report.finalFindings ?? [];
    return report;
  } catch {
    return null;
  }
}

/**
 * Ensures configured directory junctions exist. Worktrees live outside the
 * source repository, so sibling references like ../../king-v.core break unless
 * a junction re-points them at the real directory (created once, idempotent).
 */
async function ensureJunctions(config) {
  if (!Array.isArray(config.junctions)) {
    return;
  }
  for (const junction of config.junctions) {
    const linkPath = path.resolve(config.worktreeRoot, junction.link);
    if (await pathExists(linkPath)) {
      continue;
    }
    await mkdir(path.dirname(linkPath), { recursive: true });
    await symlink(
      junction.target,
      linkPath,
      process.platform === "win32" ? "junction" : "dir"
    );
  }
}

async function pathExists(value) {
  try {
    await lstat(value);
    return true;
  } catch {
    return false;
  }
}

function ensureAllowedRepository(config, repoRoot) {
  if (!config.allowedRepositories.includes(normalizePath(repoRoot))) {
    throw new Error(`仓库不在 allowedRepositories 白名单中: ${repoRoot}`);
  }
}

async function validateRepository(config, repoRoot, baseRef, runRoot) {
  ensureSafeBaseRef(baseRef);
  const topLevel = (
    await runGit(
      repoRoot,
      ["rev-parse", "--show-toplevel"],
      path.join(runRoot, "repository.log")
    )
  ).stdout.trim();
  if (normalizePath(topLevel) !== normalizePath(repoRoot)) {
    throw new Error(`repoRoot 不是 Git 仓库根目录: ${repoRoot}`);
  }
  await runGit(
    repoRoot,
    ["rev-parse", "--verify", `${baseRef}^{commit}`],
    path.join(runRoot, "base-ref.log")
  );
  if (config.requireCleanRepository) {
    const status = (
      await runGit(
        repoRoot,
        ["status", "--porcelain"],
        path.join(runRoot, "source-status.log")
      )
    ).stdout.trim();
    if (status !== "") {
      throw new Error(
        "源仓库存在未提交改动。请先提交改动，或在明确理解风险后关闭 requireCleanRepository"
      );
    }
  }
}

async function createWorktree(
  repoRoot,
  worktreePath,
  branch,
  baseRef,
  runRoot
) {
  if (await directoryExists(worktreePath)) {
    throw new Error(`目标 worktree 已存在: ${worktreePath}`);
  }
  await mkdir(path.dirname(worktreePath), { recursive: true });
  await runGit(
    repoRoot,
    ["worktree", "add", "-b", branch, worktreePath, baseRef],
    path.join(runRoot, "create-worktree.log")
  );
}

async function runDeveloper(options) {
  const prompt = buildDeveloperPrompt(
    options.task,
    options.feedback,
    options.round
  );
  return await runAgent({
    agent: options.config.developer,
    prompt,
    role: "developer",
    ...options
  });
}

async function runReviewer(options) {
  const before = await getWorktreeFingerprint(
    options.worktreePath,
    options.runRoot,
    `review-${options.round}-before`
  );
  const prompt = buildReviewPrompt(options.task);
  const processResult = await runAgent({
    agent: options.config.reviewer,
    prompt,
    role: "reviewer",
    ...options
  });
  const after = await getWorktreeFingerprint(
    options.worktreePath,
    options.runRoot,
    `review-${options.round}-after`
  );
  const blockingIssues = [];
  let parsed = null;

  if (!isSuccessful(processResult)) {
    blockingIssues.push({
      source: "reviewer",
      message: compactFailure(processResult),
      logPath: processResult.logPath
    });
  } else if (before !== after) {
    blockingIssues.push({
      source: "reviewer",
      message: "只读审查器修改了 worktree，流水线已拒绝该轮结果"
    });
  } else {
    try {
      parsed = parseReviewResult(processResult.stdout);
      const blockingSeverities = new Set(options.config.blockingSeverities);
      parsed.findings
        .filter((finding) => blockingSeverities.has(finding.severity))
        .forEach((finding) => blockingIssues.push(finding));
      if (!parsed.approved && blockingIssues.length === 0) {
        blockingIssues.push({
          source: "reviewer",
          message: parsed.summary || "审查器未批准本轮改动"
        });
      }
    } catch (error) {
      blockingIssues.push({
        source: "reviewer",
        message: `无法解析结构化审查结果: ${
          error instanceof Error ? error.message : String(error)
        }`,
        logPath: processResult.logPath
      });
    }
  }

  return {
    succeeded: isSuccessful(processResult),
    durationMs: processResult.durationMs,
    logPath: processResult.logPath,
    result: parsed,
    blockingIssues
  };
}

async function runVerification(config, worktreePath, runRoot, round) {
  const results = [];
  for (const step of config.verification) {
    const result = await runProcess({
      command: step.command,
      args: step.args,
      cwd: worktreePath,
      env: step.env,
      timeoutSeconds: step.timeoutSeconds,
      logPath: path.join(
        runRoot,
        `round-${round}-verify-${safeName(step.name)}.log`
      )
    });
    results.push({
      name: step.name,
      succeeded: isSuccessful(result),
      exitCode: result.exitCode,
      timedOut: result.timedOut,
      durationMs: result.durationMs,
      logPath: result.logPath,
      failureSummary: isSuccessful(result) ? "" : compactFailure(result)
    });
    if (!isSuccessful(result)) {
      break;
    }
  }
  return results;
}

async function runAgent(options) {
  const promptPath = path.join(
    options.runRoot,
    `round-${options.round}-${options.role}-prompt.md`
  );
  await writeFile(promptPath, options.prompt, "utf8");
  const replacements = {
    "{prompt}": options.prompt,
    "{promptFile}": promptPath,
    "{promptFileWsl}": toWslPath(promptPath),
    "{worktreePath}": options.worktreePath,
    "{worktreePathWsl}": toWslPath(options.worktreePath),
    "{taskId}": options.taskId,
    "{round}": String(options.round)
  };
  const args = options.agent.args.map((argument) =>
    replacePlaceholders(argument, replacements)
  );
  return await runProcess({
    command: options.agent.command,
    args,
    cwd: options.worktreePath,
    env: options.agent.env,
    stdin: options.agent.promptMode === "stdin" ? options.prompt : undefined,
    timeoutSeconds: options.agent.timeoutSeconds,
    logPath: path.join(
      options.runRoot,
      `round-${options.round}-${options.role}.log`
    )
  });
}

function buildDeveloperPrompt(task, feedback, round) {
  const rework = feedback
    ? `\n\n这是第 ${round} 轮返工。必须逐项修复以下验收问题：\n${feedback}`
    : "";
  return `你是该仓库的实现工程师。请在当前 worktree 内完成任务，严格遵守仓库中的 AGENTS.md 和模块 README。

任务：
${task}

要求：
1. 先阅读适用的 AGENTS.md、README 和现有实现。
2. 只修改任务需要的文件，不回退或覆盖其他合理改动。
3. 同步实体、DTO、SQL、接口和文档。
4. 自行执行必要的局部验证，但不要创建 Git 提交。
5. 完成后简要说明改动与验证结果。${rework}`;
}

function buildReviewPrompt(task) {
  return `你是只读代码审查员。只允许读取和分析当前 worktree，禁止编辑、写入、格式化或生成文件。

原始任务：
${task}

请结合 AGENTS.md、模块 README、git diff 和现有测试进行审查，重点检查：
- 业务逻辑错误、状态流转、并发、事务和数据一致性；
- 接口、DTO、实体、SQL、文档是否同步；
- 重复代码、无用引用、项目既有扩展方法和 LINQ 规范；
- 缺失或无效测试。

最终输出必须只包含下面两个标记及其中的 JSON，不要输出 Markdown 代码块：
${REVIEW_START}
{
  "approved": true,
  "summary": "简短结论",
  "findings": [
    {
      "severity": "P0|P1|P2|P3",
      "title": "问题标题",
      "file": "相对路径",
      "line": 1,
      "description": "问题说明",
      "suggestedFix": "修复建议"
    }
  ]
}
${REVIEW_END}

存在 P0 或 P1 时 approved 必须为 false；没有问题时 findings 返回空数组。`;
}

function parseReviewResult(stdout) {
  const normalized = stripAnsi(stdout);
  const start = normalized.lastIndexOf(REVIEW_START);
  const end = normalized.lastIndexOf(REVIEW_END);
  if (start < 0 || end <= start) {
    throw new Error("缺少审查结果标记");
  }
  const json = normalized
    .slice(start + REVIEW_START.length, end)
    .trim();
  const result = JSON.parse(json);
  if (
    typeof result.approved !== "boolean" ||
    typeof result.summary !== "string" ||
    !Array.isArray(result.findings)
  ) {
    throw new Error("审查结果字段不完整");
  }
  result.findings.forEach((finding) => {
    if (!["P0", "P1", "P2", "P3"].includes(finding.severity)) {
      throw new Error(`未知严重级别: ${finding.severity}`);
    }
  });
  return result;
}

function buildReworkFeedback(roundReport) {
  return roundReport.blockingIssues
    .map((issue, index) => {
      const location = issue.file
        ? ` (${issue.file}${issue.line ? `:${issue.line}` : ""})`
        : "";
      return `${index + 1}. [${issue.severity ?? issue.source}] ${
        issue.title ?? issue.message
      }${location}${issue.description ? `\n${issue.description}` : ""}`;
    })
    .join("\n");
}

async function getWorktreeFingerprint(worktreePath, runRoot, name) {
  const status = await runGit(
    worktreePath,
    ["status", "--porcelain=v1", "--untracked-files=all"],
    path.join(runRoot, `${name}-status.log`)
  );
  const diff = await runGit(
    worktreePath,
    ["diff", "--binary", "HEAD"],
    path.join(runRoot, `${name}-diff.log`)
  );
  return createHash("sha256")
    .update(status.stdout)
    .update("\0")
    .update(diff.stdout)
    .digest("hex");
}

async function readGitLines(cwd, args, logPath) {
  const result = await runGit(cwd, args, logPath);
  return result.stdout
    .split(/\r?\n/)
    .map((item) => item.trimEnd())
    .filter(Boolean);
}

async function runGit(cwd, args, logPath) {
  const result = await runProcess({
    command: "git",
    args,
    cwd,
    timeoutSeconds: 120,
    logPath
  });
  if (!isSuccessful(result)) {
    throw new Error(`Git 命令失败: git ${args.join(" ")}\n${compactFailure(result)}`);
  }
  return result;
}

function replacePlaceholders(value, replacements) {
  return Object.entries(replacements).reduce(
    (result, [key, replacement]) => result.split(key).join(replacement),
    value
  );
}

function toWslPath(value) {
  if (process.platform !== "win32") {
    return value;
  }
  const match = /^([A-Za-z]):[\\/](.*)$/.exec(value);
  if (!match) {
    return value.replaceAll("\\", "/");
  }
  return `/mnt/${match[1].toLowerCase()}/${match[2].replaceAll("\\", "/")}`;
}

function stripAnsi(value) {
  return value.replace(
    // ANSI CSI and single-character escape sequences emitted by terminal CLIs.
    // eslint-disable-next-line no-control-regex
    /\u001B(?:[@-_]|\[[0-?]*[ -/]*[@-~])/g,
    ""
  );
}

function summarizeProcess(result) {
  return {
    succeeded: isSuccessful(result),
    exitCode: result.exitCode,
    timedOut: result.timedOut,
    durationMs: result.durationMs,
    logPath: result.logPath
  };
}

function compactFailure(result) {
  const output = `${result.stderr}\n${result.stdout}`.trim();
  const tail = output.slice(-4000);
  if (result.timedOut) {
    return `执行超时。${tail}`;
  }
  return `退出码 ${result.exitCode ?? "unknown"}。${tail}`;
}

function isSuccessful(result) {
  return result.exitCode === 0 && !result.timedOut;
}

function clampReworkRounds(value) {
  if (value === undefined) {
    return MAX_REWORK_ROUNDS;
  }
  if (!Number.isInteger(value) || value < 0 || value > MAX_REWORK_ROUNDS) {
    throw new Error(`maxReworkRounds 必须是 0-${MAX_REWORK_ROUNDS} 的整数`);
  }
  return value;
}

function createTaskId(task) {
  const timestamp = new Date()
    .toISOString()
    .replace(/[-:TZ.]/g, "")
    .slice(0, 14);
  const digest = createHash("sha256").update(task).digest("hex").slice(0, 8);
  const random = randomUUID().replaceAll("-", "").slice(0, 8);
  return `${timestamp}-${digest}-${random}`;
}

function safeName(value) {
  return value.replace(/[^a-zA-Z0-9_-]/g, "-");
}

function ensureSafeBaseRef(baseRef) {
  if (
    !/^[A-Za-z0-9][A-Za-z0-9._/-]{0,199}$/.test(baseRef) ||
    baseRef.includes("..") ||
    baseRef.includes("@{")
  ) {
    throw new Error(`baseRef 格式不安全: ${baseRef}`);
  }
}

async function directoryExists(value) {
  try {
    const entry = await stat(value);
    return entry.isDirectory();
  } catch {
    return false;
  }
}

export const pipelineInternals = {
  buildDeveloperPrompt,
  buildReviewPrompt,
  parseReviewResult,
  clampReworkRounds,
  replacePlaceholders,
  ensureSafeBaseRef,
  toWslPath
};
