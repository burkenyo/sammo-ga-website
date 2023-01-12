/// <reference types="../global" />

import { readFileSync } from "node:fs";
import { spawnSync } from "node:child_process";

export function getOeisIdFromFile(fileName: string): Optional<string> {
  if (!fileName.endsWith(".txt")) {
    return null;
  }

  const line = readFileSync(fileName, { encoding: "utf-8" }).split(/\r?\n/g, 1)[0];
  const lineIsOeisId = line[0] == "A" && Number.isInteger(Number.parseFloat(line.substring(1)))

  return lineIsOeisId ? line : null;
}

export function invokeCommand(name: string, args: readonly string[]): Optional<string> {
  const result = spawnSync(name, args);

  if (result.status != 0) {
    throw new Error(String(result.stderr));
  }

  const output = String(result.stdout).trim();

  return output == "" ? null : output;
}
