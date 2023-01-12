/// <reference types="../global" />

import * as fs from "node:fs";

export function getOeisIdFromFile(fileName: string): Optional<string> {
  if (!fileName.endsWith(".txt")) {
    return null;
  }

  const line = fs.readFileSync(fileName, { encoding: "utf-8" }).split(/\r?\n/g, 1)[0];
  const lineIsOeisId = line[0] == "A" && Number.isInteger(Number.parseFloat(line.substring(1)))

  return lineIsOeisId ? line : null;
}
