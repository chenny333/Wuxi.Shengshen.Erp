# development-pipeline-mcp

本地 STDIO MCP 服务器：在**独立 Git worktree** 中驱动开发 Agent（opencode / MiniMax-M3）完成开发任务，自动执行编译验证，并产出结构化验收报告。

适配 `Wuxi.Shengshen.Erp`（.NET 10）仓库，验收方式为 **外部验收模式**：开发与编译通过后暂停，由 Claude（桌面端）在会话中审查 worktree diff，再通过续跑参数给出结论。

## 工作流程（reviewMode = external）

```
run_development_pipeline(task)
  → 校验仓库（白名单 / 干净 / baseRef 合法）
  → git worktree add -b pipeline/{taskId}
  → 开发 Agent 在 worktree 内实现
  → dotnet build Wuxi.Shengshen.Erp.slnx
  → 失败：把编译错误反馈给开发 Agent 返工（最多 maxReworkRounds 轮）
  → 通过：status = awaiting_review，返回 worktreePath / diffStat / changedFiles

调用方审查 worktree 后续跑：
  reviewOutcome=approved                → status = success，收尾
  reviewOutcome=rework + reworkFeedback → 原 worktree 上让开发 Agent 返工，再次 awaiting_review
```

状态机：`success` / `awaiting_review` / `rejected`（编译返工耗尽）/ `failed`（流程异常）。

`reviewMode = agent` 为旧模式：编译通过后由只读审查 Agent（指纹校验防篡改 + 标记 JSON 解析）自动验收，本仓库不使用。

## 配置（pipeline.config.json）

复制 `pipeline.config.example.json` 为 `pipeline.config.json`（已 gitignore，不进仓库）。

| 字段 | 说明 |
| --- | --- |
| `allowedRepositories` | 允许运行的仓库根目录白名单（绝对路径） |
| `worktreeRoot` | worktree 与运行日志根目录（`worktrees/{taskId}`、`runs/{taskId}`） |
| `requireCleanRepository` | 源仓库有未提交改动时拒绝运行（默认 true） |
| `branchPrefix` | worktree 分支前缀（`pipeline/`） |
| `reviewMode` | `external`（默认外部验收）或 `agent`（需另配 `reviewer`） |
| `blockingSeverities` | agent 模式下判定阻断的严重级别 |
| `junctions` | 可选。流水线启动时确保存在的目录联接：`link` 相对 worktreeRoot，`target` 为真实目录。用于修复 worktree 内的兄弟目录引用（`../../king-v.core`） |
| `developer` | 开发 Agent：`command` / `args` / `promptMode`（argument、file、stdin）/ `timeoutSeconds` / `env` |
| `reviewer` | 审查 Agent（仅 agent 模式必填） |
| `verification` | 编译/测试命令白名单，按顺序执行，失败即中断当轮 |

`args` 支持占位符：`{prompt}` `{promptFile}` `{promptFileWsl}` `{worktreePath}` `{worktreePathWsl}` `{taskId}` `{round}`。

### king-v.core 引用说明

`ApiService.csproj` / `ServiceDefaults.csproj` 用 `..\..\king-v.core\KingV.Core.csproj` 引用框架项目，worktree 落在 `wuxi-erp-worktrees\worktrees\{taskId}` 后该相对路径解析到 `wuxi-erp-worktrees\worktrees\king-v.core`。`junctions` 配置让流水线自动在此建 junction 指向真实的 `E:\Kreakin\king-v.core`（Windows junction 不需要管理员权限；只建一次，幂等）。

## 工具入参

```
run_development_pipeline({
  task,              // 新建任务：完整开发任务与验收标准（续跑时省略）
  repoRoot?,         // 默认白名单第一个
  baseRef?,          // 默认 HEAD
  maxReworkRounds?,  // 默认 3；续跑返工时默认 1
  resumeTaskId?,     // 续跑：复用该任务的 worktree 与运行目录
  reviewOutcome?,    // "approved" | "rework"（续跑必填）
  reworkFeedback?    // rework 时必填：逐条验收问题
})
```

报告同时写入 `runs/{taskId}/acceptance-report.json`；每轮的 Agent prompt、输出、编译日志、指纹都在 `runs/{taskId}/` 下。

## 接入 Claude 桌面应用

在 Claude 桌面应用的 MCP 配置（`claude_desktop_config.json`）中加入：

```json
{
  "mcpServers": {
    "development-pipeline": {
      "command": "node",
      "args": [
        "E:\\Kreakin\\Wuxi.Shengshen.Erp\\tools\\development-pipeline-mcp\\src\\index.js"
      ],
      "env": {
        "DEVELOPMENT_PIPELINE_CONFIG": "E:\\Kreakin\\Wuxi.Shengshen.Erp\\tools\\development-pipeline-mcp\\pipeline.config.json"
      }
    }
  }
}
```

前提：Windows 侧 `node` 在 PATH 中；首次运行前在本目录执行过 `npm install`；`wsl`（Ubuntu-24.04 + opencode）与 `dotnet` 可用。

## 注意事项

- 流水线要求源仓库**干净**（tools/ 目录已 gitignore 的内容除外）——运行前先提交或暂存改动。
- worktree 内禁止开发 Agent 创建 Git 提交（prompt 已约束），验收通过后由人决定合并/丢弃。
- 清理：`git worktree remove <path>` + 删除对应 `pipeline/{taskId}` 分支与 `runs/{taskId}` 目录。

## 开发

```bash
npm install
npm test   # node --test，含 mock Agent 的端到端用例（外部验收 / 续跑 / 返工）
```
