/// <reference types="../global" />

import * as fs from "node:fs";
import path from "node:path";

import { OeisId } from "@/oeis";

// Delete the expansions files used by the MockApiRunner
fs.readdirSync("dist")
  .filter(f => f.endsWith(".txt"))
  .map(f => path.join("dist", f))
  .forEach(f => {
    try {
      const line = fs.readFileSync(f, { encoding: "utf-8" })
        .split(/\r?\n/g, 1)[0];
      OeisId.parse(line);

      // if we got here, we have an expansion file
      fs.unlinkSync(f);
    } catch { ; }
  });

// delete the expansionsList file used by the MockApiRunner
if (fs.existsSync("dist/expansionsList.json.local")) {
  fs.unlinkSync("dist/expansionsList.json.local");
}

// copy the config file used by Azure Static Web Apps
fs.copyFileSync("staticwebapp.config.json", "dist/staticwebapp.config.json");
