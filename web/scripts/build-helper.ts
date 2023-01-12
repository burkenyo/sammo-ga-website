// build-helper.ts
// handles clean-up of the output directory after after a build

import * as fs from "node:fs";
import path from "node:path";
import { getOeisIdFromFile } from "./utils";

// Delete the expansions files used by the MockApiRunner
fs.readdirSync("dist")
  .map(f => path.join("dist", f))
  .filter(f => getOeisIdFromFile(f))
  .forEach(f => fs.unlinkSync(f));

// delete the expansionsList file used by the MockApiRunner
if (fs.existsSync("dist/expansionsList.json.local")) {
  fs.unlinkSync("dist/expansionsList.json.local");
}

// copy the config file used by Azure Static Web Apps
fs.copyFileSync("staticwebapp.config.json", "dist/staticwebapp.config.json");
