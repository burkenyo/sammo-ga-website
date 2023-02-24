// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

import { assert } from "vitest";

export function testIntParamPreconditions(func: (i: number) => void, tooLow: number, tooHigh: number) {
  // should validate number is an int
  assert.throws(() => func(NaN));
  // should validate number is within the allowed range
  assert.throws(() => func(tooLow));
  assert.throws(() => func(tooHigh));
}

export function testEquals<T extends Equatable>(example: T, shouldEqualExample: T, shouldNotEqualExample: T) {
  // should not be equal under standard js reference equality
  assert.notEqual(example, shouldEqualExample);
  // should be equal under implemented value equality
  assert.isTrue(example.equals(shouldEqualExample));

  // different value
  assert.isFalse(example.equals(shouldNotEqualExample));
  // different types
  assert.isFalse(example.equals(null));
  // same shape but different type
  assert.isFalse(example.equals({ ...example }));
}
