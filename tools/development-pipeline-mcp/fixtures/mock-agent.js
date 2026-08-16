import { appendFileSync, readFileSync } from "node:fs";

const [, , role, promptPath] = process.argv;
readFileSync(promptPath, "utf8");

if (role === "developer") {
  appendFileSync("result.txt", "implemented\n", "utf8");
  process.stdout.write("development complete\n");
} else if (role === "reviewer") {
  process.stdout.write(`PIPELINE_REVIEW_RESULT_START
{"approved":true,"summary":"mock review passed","findings":[]}
PIPELINE_REVIEW_RESULT_END
`);
} else {
  process.stderr.write(`unknown role: ${role}\n`);
  process.exitCode = 1;
}
