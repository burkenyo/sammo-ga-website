// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

/// <reference types="../utility-types" />

import { spawnSync } from "node:child_process";
import { type } from "node:os";

export function invokeCommand(name: string, args: readonly string[]): Optional<string> {
  const result = spawnSync(name, args);

  if (result.status != 0) {
    throw new Error(String(result.stderr));
  }

  const output = String(result.stdout).trim();

  return output == "" ? null : output;
}

export function isRunningInGithubActions(): boolean {
  return process.env.GITHUB_ACTIONS == "true";
}

export function hasErrorCode(ex: unknown, code: string): boolean {
  if (!(ex && typeof ex == "object")) {
    return false;
  }

  if (!("code" in ex && typeof ex.code == "string")) {
    return false;
  }

  return ex.code == code;
}
