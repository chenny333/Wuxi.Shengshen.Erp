import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { fileURLToPath } from "node:url";
import { z } from "zod";
import { loadConfig } from "./config.js";
import { runDevelopmentPipeline } from "./pipeline.js";

const configPath =
  process.env.DEVELOPMENT_PIPELINE_CONFIG ??
  fileURLToPath(new URL("../pipeline.config.json", import.meta.url));
const config = await loadConfig(configPath);
const server = new McpServer({
  name: "development-pipeline",
  version: "0.1.0"
});

server.registerTool(
  "run_development_pipeline",
  {
    title: "运行隔离开发流水线",
    description:
      "在独立 Git worktree 中调用开发 Agent 并执行编译测试。reviewMode=agent 时再调用只读审查 Agent（最多返工三轮）；reviewMode=external 时开发与编译通过后返回 awaiting_review，由调用方验收后用 resumeTaskId + reviewOutcome 续跑（approved 收尾 / rework + reworkFeedback 返工）。",
    annotations: {
      readOnlyHint: false,
      destructiveHint: false,
      idempotentHint: false,
      openWorldHint: false
    },
    inputSchema: {
      task: z
        .string()
        .min(1)
        .max(30000)
        .optional()
        .describe("完整开发任务和验收标准；续跑（resumeTaskId）时省略"),
      repoRoot: z
        .string()
        .optional()
        .describe("仓库根目录，必须位于本地配置白名单"),
      baseRef: z
        .string()
        .min(1)
        .max(200)
        .optional()
        .describe("创建 worktree 的 Git 基准，默认 HEAD"),
      maxReworkRounds: z
        .number()
        .int()
        .min(0)
        .max(3)
        .optional()
        .describe("最多返工轮数，默认 3（续跑返工时默认 1）"),
      resumeTaskId: z
        .string()
        .min(1)
        .max(200)
        .optional()
        .describe("续跑已有任务：复用该任务的 worktree 与运行目录，跳过新建"),
      reviewOutcome: z
        .enum(["approved", "rework"])
        .optional()
        .describe(
          "外部验收结论：approved 收尾为成功；rework 让开发 Agent 按 reworkFeedback 返工"
        ),
      reworkFeedback: z
        .string()
        .max(30000)
        .optional()
        .describe("reviewOutcome=rework 时的验收问题清单，逐条列出")
    }
  },
  async (input) => {
    const report = await runDevelopmentPipeline(config, input);
    return {
      content: [
        {
          type: "text",
          text: JSON.stringify(report, null, 2)
        }
      ],
      structuredContent: report,
      isError: report.status === "failed"
    };
  }
);

await server.connect(new StdioServerTransport());
