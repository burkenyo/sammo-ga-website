// scaffold-mocks.ts
// handles scaffolding any files that are needed for mocking services during local testing

import * as fs from "node:fs";
import path from "node:path";
import { getOeisIdFromFile } from "./utils";

// Create the list of expansions used by the MockApiRunner
const idStrings = fs.readdirSync("public")
  .map(f => getOeisIdFromFile(path.join("public", f)))
  .filter(i => i)

fs.writeFileSync(path.join("public", "expansionsList.json.local"), JSON.stringify(idStrings));
