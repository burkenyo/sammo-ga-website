// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

// build-helper.ts
// handles clean-up of the output directory after after a build

import * as fs from "node:fs";
import { hasErrorCode, isRunningInGithubActions } from "./utils";

if (isRunningInGithubActions()) {
  console.log("build-helper: GitHub Actions detected!");
}

// delete the expansions files used by the MockApiRunner
fs.rmSync("dist/expansions", { recursive: true, force: true });

// delete the expansionsList file used by the MockApiRunner
fs.rmSync("dist/expansionsList.json.local", { force: true });

// copy the config file used by Azure Static Web Apps
fs.copyFileSync("staticwebapp.config.json", "dist/staticwebapp.config.json");

if (!isRunningInGithubActions()) {
  // If we are building locally, create a 404 file so that the Node-based http-server will see it.
  // If http-server is updated to allow a custom name for the 404 file, use that setting and remove this.
  // https://github.com/http-party/http-server/issues/678

  // Note, I cannot seem to find a plugin hook that will be called after vite-ssg has rendered the pages.
  // Therefore, use a symlink instead of a copy to prevent a file-not-found error.

  try {
    fs.symlinkSync("notfound.html", "dist/404.html");
  } catch (ex: unknown) {
    if (!hasErrorCode(ex, "EEXIST")) {
      throw ex;
    }
  }
}
