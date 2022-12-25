// scaffold-mocks.ts
// handles scaffolding any files that are needed for mocking services during local testing

/// <reference types="../global" />

import * as fs from "node:fs";
import path from "node:path";

import { OeisId } from "@/oeis";

// Create the list of expansions used by the MockApiRunner
const idStrings = fs.readdirSync("public")
  .filter(f => f.endsWith(".txt"))
  .map(f => {
    try {
      const line = fs.readFileSync(path.join("public", f), { encoding: "utf-8" })
        .split(/\r?\n/g, 1)[0];
      return String(OeisId.parse(line));
    } catch {
      return null;
    }
  })
  .filter(i => i)

fs.writeFileSync(path.join("public", "expansionsList.json.local"), JSON.stringify(idStrings));
