// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

import { test, assert } from "vitest";
import { testEquals, testIntParamPreconditions } from "./helpers";
import { OeisId } from "@/oeis";

const A000796 = new OeisId(796);

const A001622 = new OeisId(1622);

const A1234567 = new OeisId(1234567);

test("OeisId_ContructorWithGarbage_Throws", () => {
  testIntParamPreconditions(i => new OeisId(i), 0, OeisId.MAX_VALUE + 1);
});

test("OeisId_Constructor_SetsValue", () => {
  assert.strictEqual(A000796.value, 796);
  assert.strictEqual(A001622.value, 1622);
  assert.strictEqual(A1234567.value, 1234567);
});

test("OeisID_ToString_FollowsExpectedFormat", () => {
  assert.strictEqual(String(A000796), "A000796");
  assert.strictEqual(String(A001622), "A001622");
  assert.strictEqual(String(A1234567), "A1234567");
});

test("OeisId_ParseWithGarbage_Throws", () => {
  // Doesn’t match format
  assert.throws(() => OeisId.parse("A000D78"));
  // Empty string
  assert.throws(() => OeisId.parse(""));
  // Value out of range
  assert.throws(() => OeisId.parse("0"));
  // Not an int
  assert.throws(() => OeisId.parse("12.8"));
  assert.throws(() => OeisId.parse(String(OeisId.MAX_VALUE + 1)));
});

test("OeisId_ParseVarious_ValueExtractedCorrectly", () => {
  assert.strictEqual(A000796.value, OeisId.parse("A000796").value);
  assert.strictEqual(A000796.value, OeisId.parse("796").value);
  assert.strictEqual(A001622.value, OeisId.parse("A001622").value);
  assert.strictEqual(A001622.value, OeisId.parse("1622").value);
  assert.strictEqual(A1234567.value, OeisId.parse("A1234567").value);
  assert.strictEqual(A1234567.value, OeisId.parse("1234567").value);
});

test("OeisId_EqualsVarious_ReturnExpected", () => {
  testEquals(A000796, new OeisId(A000796.value), A001622);
});
