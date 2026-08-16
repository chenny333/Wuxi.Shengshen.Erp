import assert from "node:assert/strict";
import { mkdir, readFile, rm, stat, writeFile } from "node:fs/promises";
import path from "node:path";
import { spawnSync } from "node:child_process";
import test from "node:test";
import { fileURLToPath } from "node:url";
import {
  pipelineInternals,
  runDevelopmentPipeline
} from "../src/pipeline.js";
import { loadConfig } from "../src/config.js";
import { isWindowsBatchCommand } from "../src/process-runner.js";

const testDirectory = path.dirname(fileURLToPath(import.meta.url));

test("parseReviewResult parses marked JSON", () => {
  const result = pipelineInternals.parseReviewResult(`
noise
PIPELINE_REVIEW_RESULT_START
{"approved":false,"summary":"has issue","findings":[{"severity":"P1","title":"bug"}]}
PIPELINE_REVIEW_RESULT_END
`);
  assert.equal(result.approved, false);
  assert.equal(result.findings[0].severity, "P1");
});

test("parseReviewResult rejects unknown severity", () => {
  assert.throws(
    () =>
      pipelineInternals.parseReviewResult(`
PIPELINE_REVIEW_RESULT_START
{"approved":false,"summary":"bad","findings":[{"severity":"HIGH"}]}
PIPELINE_REVIEW_RESULT_END
`),
    /未知严重级别/
  );
});

test("clampReworkRounds caps accepted values at three", () => {
  assert.equal(pipelineInternals.clampReworkRounds(undefined), 3);
  assert.equal(pipelineInternals.clampReworkRounds(0), 0);
  assert.equal(pipelineInternals.clampReworkRounds(3), 3);
  assert.throws(() => pipelineInternals.clampReworkRounds(4), /0-3/);
});

test("replacePlaceholders replaces all occurrences", () => {
  assert.equal(
    pipelineInternals.replacePlaceholders(
      "{round}:{promptFile}:{round}",
      { "{round}": "2", "{promptFile}": "prompt.md" }
    ),
    "2:prompt.md:2"
  );
});

test("toWslPath maps Windows drive paths", () => {
  const source = "E:\\Kreakin\\bbk-api-worktrees\\runs\\prompt.md";
  const expected =
    process.platform === "win32"
      ? "/mnt/e/Kreakin/bbk-api-worktrees/runs/prompt.md"
      : source;
  assert.equal(pipelineInternals.toWslPath(source), expected);
});

test("ensureSafeBaseRef rejects option-like and revision expressions", () => {
  pipelineInternals.ensureSafeBaseRef("feature/member-level");
  assert.throws(() => pipelineInternals.ensureSafeBaseRef("--help"), /不安全/);
  assert.throws(() => pipelineInternals.ensureSafeBaseRef("HEAD..main"), /不安全/);
  assert.throws(() => pipelineInternals.ensureSafeBaseRef("HEAD@{1}"), /不安全/);
});

test("isWindowsBatchCommand detects Windows batch commands only on Windows", () => {
  assert.equal(
    isWindowsBatchCommand("E:\\tools\\mvn.cmd"),
    process.platform === "win32"
  );
  assert.equal(
    isWindowsBatchCommand("E:\\tools\\verify.BAT"),
    process.platform === "win32"
  );
  assert.equal(isWindowsBatchCommand("E:\\tools\\java.exe"), false);
});

test("loadConfig requires reviewer only in agent reviewMode", async () => {
  const tempRoot = path.join(testDirectory, "..", ".test-tmp", "config");
  await rm(tempRoot, { recursive: true, force: true });
  await mkdir(tempRoot, { recursive: true });
  const configPath = path.join(tempRoot, "config.json");
  const base = {
    allowedRepositories: ["E:\\repo"],
    worktreeRoot: path.join(tempRoot, "wt"),
    branchPrefix: "pipeline/",
    developer: { command: "node", args: ["agent.js"], promptMode: "file" },
    verification: [{ name: "verify", command: "node", args: ["-e", ""] }]
  };
  try {
    await writeFile(
      configPath,
      JSON.stringify({ ...base, reviewMode: "external" }),
      "utf8"
    );
    const external = await loadConfig(configPath);
    assert.equal(external.reviewMode, "external");

    await writeFile(configPath, JSON.stringify(base), "utf8");
    await assert.rejects(() => loadConfig(configPath), /reviewer/);

    await writeFile(
      configPath,
      JSON.stringify({ ...base, reviewMode: "nope" }),
      "utf8"
    );
    await assert.rejects(() => loadConfig(configPath), /reviewMode/);
  } finally {
    await rm(tempRoot, { recursive: true, force: true });
  }
});

test("runDevelopmentPipeline completes an isolated mock pipeline", async () => {
  const { tempRoot, repoRoot, worktreeRoot } = await createRepoFixture(
    "integration"
  );
  const config = createMockConfig(repoRoot, worktreeRoot, {
    reviewer: {
      command: process.execPath,
      args: [mockAgentPath(), "reviewer", "{promptFile}"],
      promptMode: "file",
      timeoutSeconds: 30,
      env: {}
    }
  });

  try {
    const report = await runDevelopmentPipeline(config, {
      task: "create result",
      repoRoot,
      maxReworkRounds: 0
    });
    assert.equal(report.status, "success");
    assert.equal(report.rounds.length, 1);
    assert.deepEqual(report.finalFindings, []);
    assert.ok(report.changedFiles.some((item) => item.includes("result.txt")));
  } finally {
    await rm(tempRoot, { recursive: true, force: true });
  }
});

test("external reviewMode awaits review then resumes to approved", async () => {
  const { tempRoot, repoRoot, worktreeRoot } = await createRepoFixture(
    "external"
  );
  const config = createMockConfig(repoRoot, worktreeRoot, {
    reviewMode: "external",
    junctions: [{ link: "worktrees/king-v.core", target: repoRoot }]
  });

  try {
    const first = await runDevelopmentPipeline(config, {
      task: "create result",
      repoRoot,
      maxReworkRounds: 0
    });
    assert.equal(first.status, "awaiting_review");
    assert.equal(first.rounds.length, 1);
    assert.equal(first.rounds[0].review, null);
    assert.ok(first.changedFiles.some((item) => item.includes("result.txt")));

    const junction = await stat(
      path.join(worktreeRoot, "worktrees", "king-v.core")
    );
    assert.ok(junction.isDirectory());

    const state = JSON.parse(
      await readFile(path.join(first.runRoot, "pipeline-state.json"), "utf8")
    );
    assert.equal(state.task, "create result");
    assert.equal(state.repoRoot, repoRoot);

    const approved = await runDevelopmentPipeline(config, {
      resumeTaskId: first.taskId,
      reviewOutcome: "approved"
    });
    assert.equal(approved.status, "success");
    assert.equal(approved.message, "外部验收通过");
    assert.equal(approved.taskId, first.taskId);
    assert.equal(approved.externalReviews.length, 1);
    assert.equal(approved.externalReviews[0].outcome, "approved");
  } finally {
    await rm(tempRoot, { recursive: true, force: true });
  }
});

test("external reviewMode rework reruns developer on the same worktree", async () => {
  const { tempRoot, repoRoot, worktreeRoot } = await createRepoFixture(
    "external-rework"
  );
  const config = createMockConfig(repoRoot, worktreeRoot, {
    reviewMode: "external"
  });

  try {
    const first = await runDevelopmentPipeline(config, {
      task: "create result",
      repoRoot,
      maxReworkRounds: 0
    });
    assert.equal(first.status, "awaiting_review");

    const rework = await runDevelopmentPipeline(config, {
      resumeTaskId: first.taskId,
      reviewOutcome: "rework",
      reworkFeedback: "补上缺失的 XML 文档注释"
    });
    assert.equal(rework.status, "awaiting_review");
    assert.equal(rework.taskId, first.taskId);
    assert.equal(rework.worktreePath, first.worktreePath);
    assert.equal(rework.rounds.length, 2);
    assert.equal(rework.developerRuns.length, 2);
    assert.equal(rework.externalReviews[0].outcome, "rework");

    const resultFile = await readFile(
      path.join(first.worktreePath, "result.txt"),
      "utf8"
    );
    assert.equal(resultFile.match(/implemented/g).length, 2);
  } finally {
    await rm(tempRoot, { recursive: true, force: true });
  }
});

test("resume rejects invalid input before touching the filesystem", async () => {
  const config = createMockConfig("E:\\repo", "E:\\wt", {
    reviewMode: "external"
  });
  await assert.rejects(
    () =>
      runDevelopmentPipeline(config, {
        resumeTaskId: "../evil",
        reviewOutcome: "approved"
      }),
    /格式不合法/
  );
  await assert.rejects(
    () => runDevelopmentPipeline(config, { resumeTaskId: "abc-123" }),
    /reviewOutcome/
  );
  await assert.rejects(
    () =>
      runDevelopmentPipeline(config, {
        resumeTaskId: "abc-123",
        reviewOutcome: "rework"
      }),
    /reworkFeedback/
  );
  await assert.rejects(() => runDevelopmentPipeline(config, {}), /task/);
});

async function createRepoFixture(name) {
  const tempRoot = path.join(testDirectory, "..", ".test-tmp", name);
  const repoRoot = path.join(tempRoot, "source");
  const worktreeRoot = path.join(tempRoot, "pipeline");
  await rm(tempRoot, { recursive: true, force: true });
  await mkdir(repoRoot, { recursive: true });
  await writeFile(path.join(repoRoot, "README.md"), "fixture\n", "utf8");

  git(repoRoot, ["init"]);
  git(repoRoot, ["add", "README.md"]);
  git(repoRoot, [
    "-c",
    "user.name=Pipeline Test",
    "-c",
    "user.email=pipeline@example.test",
    "commit",
    "-m",
    "init"
  ]);
  return { tempRoot, repoRoot, worktreeRoot };
}

function createMockConfig(repoRoot, worktreeRoot, extra = {}) {
  return {
    allowedRepositories: [normalize(repoRoot)],
    worktreeRoot,
    requireCleanRepository: true,
    branchPrefix: "pipeline/",
    blockingSeverities: ["P0", "P1"],
    developer: {
      command: process.execPath,
      args: [mockAgentPath(), "developer", "{promptFile}"],
      promptMode: "file",
      timeoutSeconds: 30,
      env: {}
    },
    verification: [
      {
        name: "verify",
        command: process.execPath,
        args: ["-e", "process.exit(0)"],
        timeoutSeconds: 30
      }
    ],
    ...extra
  };
}

function mockAgentPath() {
  return path.join(testDirectory, "..", "fixtures", "mock-agent.js");
}

function git(cwd, args) {
  const result = spawnSync("git", args, {
    cwd,
    encoding: "utf8",
    windowsHide: true
  });
  assert.equal(
    result.status,
    0,
    `git ${args.join(" ")} failed:\n${result.stderr}`
  );
}

function normalize(value) {
  const resolved = path.resolve(value);
  return process.platform === "win32" ? resolved.toLowerCase() : resolved;
}
