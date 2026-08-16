import { readFile } from "node:fs/promises";
import path from "node:path";

const PROMPT_MODES = new Set(["argument", "file", "stdin"]);
const REVIEW_MODES = new Set(["agent", "external"]);

/**
 * Loads and validates the local pipeline configuration.
 *
 * @param {string} configPath configuration file path
 * @returns {Promise<Record<string, any>>} validated configuration
 */
export async function loadConfig(configPath) {
  const absolutePath = path.resolve(configPath);
  const config = JSON.parse(await readFile(absolutePath, "utf8"));

  ensureNonEmptyArray(config.allowedRepositories, "allowedRepositories");
  ensureString(config.worktreeRoot, "worktreeRoot");
  ensureString(config.branchPrefix, "branchPrefix");
  config.reviewMode = config.reviewMode ?? "agent";
  if (!REVIEW_MODES.has(config.reviewMode)) {
    throw new Error('reviewMode 必须是 "agent" 或 "external"');
  }
  ensureAgent(config.developer, "developer");
  if (config.reviewMode === "agent" || config.reviewer !== undefined) {
    ensureAgent(config.reviewer, "reviewer");
  }
  ensureNonEmptyArray(config.verification, "verification");
  config.verification.forEach((step, index) => {
    ensureString(step.name, `verification[${index}].name`);
    ensureString(step.command, `verification[${index}].command`);
    ensureStringArray(step.args, `verification[${index}].args`);
  });
  if (config.junctions !== undefined) {
    if (!Array.isArray(config.junctions)) {
      throw new Error("junctions 必须是数组");
    }
    config.junctions.forEach((junction, index) => {
      ensureString(junction.link, `junctions[${index}].link`);
      ensureString(junction.target, `junctions[${index}].target`);
    });
  }

  config.allowedRepositories = config.allowedRepositories.map((item) =>
    normalizePath(item)
  );
  config.worktreeRoot = path.resolve(config.worktreeRoot);
  config.requireCleanRepository = config.requireCleanRepository !== false;
  config.blockingSeverities = config.blockingSeverities ?? ["P0", "P1"];
  return config;
}

/**
 * Normalizes a path for safe case-insensitive comparison on Windows.
 *
 * @param {string} value source path
 * @returns {string} normalized path
 */
export function normalizePath(value) {
  const normalized = path.resolve(value);
  return process.platform === "win32" ? normalized.toLowerCase() : normalized;
}

function ensureAgent(agent, name) {
  if (!agent || typeof agent !== "object") {
    throw new Error(`${name} 配置不能为空`);
  }
  ensureString(agent.command, `${name}.command`);
  ensureStringArray(agent.args, `${name}.args`);
  if (!PROMPT_MODES.has(agent.promptMode)) {
    throw new Error(
      `${name}.promptMode 必须是 argument、file 或 stdin`
    );
  }
}

function ensureString(value, name) {
  if (typeof value !== "string" || value.trim() === "") {
    throw new Error(`${name} 必须是非空字符串`);
  }
}

function ensureStringArray(value, name) {
  if (!Array.isArray(value) || value.some((item) => typeof item !== "string")) {
    throw new Error(`${name} 必须是字符串数组`);
  }
}

function ensureNonEmptyArray(value, name) {
  if (!Array.isArray(value) || value.length === 0) {
    throw new Error(`${name} 必须是非空数组`);
  }
}
