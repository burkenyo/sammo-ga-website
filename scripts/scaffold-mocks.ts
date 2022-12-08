// scaffold-mocks.ts
// handles scaffolding any files that are needed for mocking services during local testing

import * as fs from "node:fs";
import path from "node:path";

import { OeisFractionalExpansion } from "@/services/oeis";

// Create the list of expansions used by the MockApiRunner
const idStrings = fs.readdirSync("public")
  .filter(f => f.endsWith(".txt"))
  .map(f => {
    const content = fs.readFileSync(path.join("public", f), { encoding: "utf-8" });
    const expansion = OeisFractionalExpansion.parseRawText(content);

    return String(expansion.id);
  });

fs.writeFileSync(path.join("public", "expansionsList.json.local"), JSON.stringify(idStrings));
