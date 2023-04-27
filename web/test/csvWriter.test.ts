// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

import { test, assert } from "vitest";
import { CsvWriter } from "@vote-demo/csvWriter";

test("CsvWriter_noFieldsProvided_throws", () => {
  assert.throws(() => new CsvWriter(null!));
  assert.throws(() => new CsvWriter([]));
});

test("CsvWriter_getBlob_generatesExpectedResult", async () => {
  const { headers, rows, expected } = getMockData();

  const writer = new CsvWriter(headers);

  for(const row of rows) {
    writer.writeRow(row);
  }

  assert.strictEqual(await writer.getBlob().text(), expected);
});

// Test that a writer can be written to followed by reading more than once.
test("CsvWriter_getBlobTwice_generatesExpectedResult", async () => {
  const writer = new CsvWriter(["foo", "bar", "baz"]);

  assert.strictEqual(await writer.getBlob().text(), "foo,bar,baz\r\n");

  writer.writeRow({ foo: 13, bar: true, baz: 47n });

  assert.strictEqual(await writer.getBlob().text(), "foo,bar,baz\r\n13,true,47\r\n");
});

// generate a large amount of CSV data
function getMockData(): {
  headers: readonly string[], rows: { [key: string]: (string | number | null | bigint | boolean) }[], expected: string
} {
  // dummy column names that include commas and quotes
  const headers = Array(10)
  .fill(["foo", "\"foo\", 1", "bar", "bar\"", "baz,", "baz, 1"])
  .map((r: string[], i) => r.map(v => v + i))
  .flat();

  function swap(a: number, b: number) {
    [headers[b], headers[a]] = [headers[a], headers[b]];
  }

  // move some columns so it’s not completely regular
  swap(17, 34);
  swap(2, 49);
  swap(30, 50);

  // dummy values that include all supported primitives and strings with commas and quotes
  const rowTemplate = Array(5)
    .fill(["foo", "\"foo\", 1", "bar", "bar\"", "baz,", "baz, 1", true, false, null, -2, 89.5, 30_000_000n]).flat();

  const row = [...rowTemplate];
  const rowArrays: any[][] = Array(27).fill(row);

  function addShiftedRows(amount: number, shift: number) {
    const shifted = [...rowTemplate];
    shifted.splice(0, shift);
    shifted.splice(shifted.length, 0, ...rowTemplate.slice(0, shift));
    rowArrays.splice(rowArrays.length, 0, ...Array(amount).fill(shifted));
  }

  // add more rows but shift them about so it's not regular
  addShiftedRows(48, 5);
  addShiftedRows(10, 3);
  addShiftedRows(15, 8);

  const rows = rowArrays.map(r => Object.fromEntries(r.map((v, i) => [headers[i], v])));

  function stringify(value: unknown): string {
    if (value == null) {
      return "";
    }

    const valueAsString = String(value);

    if (valueAsString.includes(",") || valueAsString.includes("\"")) {
      return "\"" + valueAsString.replace(/"/g, "\"\"") + "\"";
    }

    return valueAsString;
  }

  const expected = headers.map(c => stringify(c)).join(",") + "\r\n"
    + rowArrays.map(r => r.map(v => stringify(v)).join(",")).join("\r\n") + "\r\n";

  return { headers, rows, expected };
}
