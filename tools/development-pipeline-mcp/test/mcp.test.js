import assert from "node:assert/strict";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { Client } from "@modelcontextprotocol/sdk/client/index.js";
import { StdioClientTransport } from "@modelcontextprotocol/sdk/client/stdio.js";

const testDirectory = path.dirname(fileURLToPath(import.meta.url));

test("MCP server exposes run_development_pipeline", async () => {
  const serverPath = path.join(testDirectory, "..", "src", "index.js");
  const configPath = path.join(
    testDirectory,
    "..",
    "pipeline.config.example.json"
  );
  const transport = new StdioClientTransport({
    command: process.execPath,
    args: [serverPath],
    env: {
      ...process.env,
      DEVELOPMENT_PIPELINE_CONFIG: configPath
    }
  });
  const client = new Client({
    name: "development-pipeline-test",
    version: "0.1.0"
  });

  try {
    await client.connect(transport);
    const result = await client.listTools();
    assert.ok(
      result.tools.some((tool) => tool.name === "run_development_pipeline")
    );
  } finally {
    await client.close();
  }
});
