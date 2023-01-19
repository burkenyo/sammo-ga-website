// build-helper.ts
// handles clean-up of the output directory after after a build

import * as fs from "node:fs";
import path from "node:path";
import { getOeisIdFromFile, isRunningInGithubActions } from "./utils";

if (isRunningInGithubActions()) {
  console.log("build-helper: GitHub Actions detected!");
}

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

if (!isRunningInGithubActions()) {
  // If we are building locally, create a 404 file so that the Node-based http-server will see it.
  // If http-server is updated to allow a custom name for the 404 file, use that setting and remove this.
  // https://github.com/http-party/http-server/issues/678

  // Note, I cannot seem to find a plugin hook that will be called after vite-ssg has rendered the pages.
  // Therefore, use a symlink instead of a copy to prevent a file-not-found error.

  fs.symlinkSync("notfound.html", "dist/404.html");
}
