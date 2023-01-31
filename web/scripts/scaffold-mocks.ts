// scaffold-mocks.ts
// handles scaffolding any files that are needed for mocking services during local testing

import * as fs from "node:fs";
import path from "node:path";

// Create the list of expansions used by the MockApiRunner
const idStrings = fs.readdirSync("public/expansions")
  .map(f => getOeisIdFromFile(path.join("public/expansions", f)))
  .filter(i => i)

fs.writeFileSync(path.join("public", "expansionsList.json.local"), JSON.stringify(idStrings));


function getOeisIdFromFile(fileName: string): Optional<string> {
  if (!fileName.endsWith(".txt")) {
    return null;
  }

  const line = fs.readFileSync(fileName, { encoding: "utf-8" }).split(/\r?\n/g, 1)[0];
  const lineIsOeisId = line[0] == "A" && Number.isInteger(Number.parseFloat(line.substring(1)))

  return lineIsOeisId ? line : null;
}
