import { spawn } from "node:child_process";
import { createWriteStream } from "node:fs";
import { mkdir } from "node:fs/promises";
import path from "node:path";

const MAX_CAPTURE_CHARACTERS = 2_000_000;

/**
 * Executes one configured process. Windows batch commands are delegated to
 * the system command shell because Node cannot spawn them directly.
 *
 * @param {{
 *   command: string,
 *   args?: string[],
 *   cwd: string,
 *   env?: Record<string, string>,
 *   stdin?: string,
 *   timeoutSeconds?: number,
 *   logPath: string
 * }} options process options
 * @returns {Promise<{
 *   command: string,
 *   args: string[],
 *   exitCode: number | null,
 *   signal: string | null,
 *   timedOut: boolean,
 *   durationMs: number,
 *   stdout: string,
 *   stderr: string,
 *   logPath: string
 * }>} process result
 */
export async function runProcess(options) {
  await mkdir(path.dirname(options.logPath), { recursive: true });
  const args = options.args ?? [];
  const startedAt = Date.now();
  let output = "";
  let errors = "";
  const log = createWriteStream(options.logPath, { flags: "w" });

  return await new Promise((resolve, reject) => {
    const child = spawn(options.command, args, {
      cwd: options.cwd,
      env: { ...process.env, ...options.env },
      shell: isWindowsBatchCommand(options.command),
      windowsHide: true,
      stdio: ["pipe", "pipe", "pipe"]
    });
    let timedOut = false;
    const timeout = setTimeout(() => {
      timedOut = true;
      child.kill("SIGTERM");
    }, Math.max(1, options.timeoutSeconds ?? 600) * 1000);

    const capture = (target, prefix) => (chunk) => {
      const text = chunk.toString();
      if (target === "stdout") {
        output = appendTail(output, text);
      } else {
        errors = appendTail(errors, text);
      }
      log.write(`${prefix}${text}`);
    };
    child.stdout.on("data", capture("stdout", ""));
    child.stderr.on("data", capture("stderr", "[stderr] "));
    child.on("error", (error) => {
      clearTimeout(timeout);
      log.end();
      reject(error);
    });
    child.on("close", (exitCode, signal) => {
      clearTimeout(timeout);
      log.end();
      resolve({
        command: options.command,
        args,
        exitCode,
        signal,
        timedOut,
        durationMs: Date.now() - startedAt,
        stdout: output,
        stderr: errors,
        logPath: options.logPath
      });
    });

    if (options.stdin !== undefined) {
      child.stdin.end(options.stdin);
    } else {
      child.stdin.end();
    }
  });
}

/**
 * Determines whether a command requires the Windows command shell.
 *
 * @param {string} command configured executable
 * @returns {boolean} true for Windows .cmd and .bat commands
 */
export function isWindowsBatchCommand(command) {
  return process.platform === "win32" && /\.(?:cmd|bat)$/i.test(command);
}

function appendTail(current, addition) {
  const combined = current + addition;
  return combined.length <= MAX_CAPTURE_CHARACTERS
    ? combined
    : combined.slice(-MAX_CAPTURE_CHARACTERS);
}
