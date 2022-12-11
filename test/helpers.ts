import { assert } from "vitest";

export function testIntParamPreconditions(func: (i: number) => void, tooLow: number, tooHigh: number) {
  // should validate number is an int
  assert.throws(() => func(NaN));
  // should validate number is within the allowed range
  assert.throws(() => func(tooLow));
  assert.throws(() => func(tooHigh));
}
