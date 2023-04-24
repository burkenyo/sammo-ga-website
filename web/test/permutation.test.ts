// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

import { test, assert } from "vitest";
import { testEquals, testIntParamPreconditions } from "./helpers";
import { Permutation } from "@melodies/permutation";

const BASE = 12;

const example = {
  base: BASE,
  number: 18258836,
  offset: 2,
  sequence: [2, 8, 3, 6, 0, 11, 7, 1, 10, 4, 9, 5],
} as const;

function create() {
  return Permutation.create(BASE, example.number, example.offset);
}

function fromSequence() {
  return Permutation.fromSequence(example.sequence);
}

function explode(permutation: Permutation) {
  // include the accessor-style properties (which don’t come automatically with the spread operator)
  return {
    ...permutation,
    base: permutation.base,
    offset: permutation.offset,
  } as const;
}

test("Permutation_GetMaxNumberWithGarbage_Throws", () => {
  testIntParamPreconditions(Permutation.getMaxNumber, 1, Permutation.MAX_BASE + 1);
});

test("Permutation_GetMaxNumber_ReturnsCorrect", () => {
  assert.strictEqual(Permutation.getMaxNumber(5), 24);

  assert.strictEqual(Permutation.getMaxNumber(8), 5040);
});

test("Permutation_CreateRandomWithGarbage_Throws", () => {
  testIntParamPreconditions(Permutation.createRandom, 1, Permutation.MAX_BASE + 1);
});

test("Permutation_CreateWithGarbage_Throws", () => {
  testIntParamPreconditions((i) => Permutation.create(i, 1, 0), 1, Permutation.MAX_BASE + 1);

  testIntParamPreconditions((i) => Permutation.create(BASE, i, 0), 0, Permutation.getMaxNumber(BASE) + 1);

  testIntParamPreconditions((i) => Permutation.create(BASE, 1, i), -1, BASE);
});

test("Permutation_Create_GeneratesCorrectSequence", () => {
  const actual = explode(create());

  assert.deepStrictEqual(actual, example);
});

test("Permutation_FromSequenceWithGarbage_Throws", () => {
  // too short
  assert.throws(() => Permutation.fromSequence([0]));

  // too long
  const sequence = Array(Permutation.MAX_BASE + 1).fill(0).map((_, i) => i);
  assert.throws(() => Permutation.fromSequence(sequence));

  // contains non-integer
  assert.throws(() => Permutation.fromSequence([0, NaN]));

  // out-of-range
  assert.throws(() => Permutation.fromSequence([1, 2]));
  assert.throws(() => Permutation.fromSequence([-1, 0]));

  // contains duplicates
  assert.throws(() => Permutation.fromSequence([0, 0, 2]));
});

test("Permuation_FromSequence_CalculatesCorrectNumber", () => {
  const actual = explode(fromSequence());

  assert.deepStrictEqual(actual, example);
});

test("Permutation_Reverse_SequenceIsReversed", () => {
  const expected = {
    base: BASE,
    number: 14299493,
    offset: 5,
    sequence: [5, 9, 4, 10, 1, 7, 11, 0, 6, 3, 8, 2], // array positions reversed from example.sequence
  };

  const actual = explode(create().reverse());

  assert.deepStrictEqual(actual, expected);
});

test("Permutation_Reflect_SequenceIsReflected", () => {
  const expected = {
    base: BASE,
    number: 21657965, // MAX_PERMUTATION - example.number + 1;
    offset: 10,
    sequence: [10, 4, 9, 6, 0, 1, 5, 11, 2, 8, 3, 7], // 12 - e for each element e (except 0 -> 0)
  };

  const actual = explode(create().reflect());

  assert.deepStrictEqual(actual, expected);
});

test("Permutation_Invert_SequenceIsInverted", () => {
  const expected = {
    base: BASE,
    number: 9735768,
    offset: 4,
    sequence: [4, 7, 0, 2, 9, 11, 3, 6, 1, 10, 8, 5], // indexes and values swapped from example.sequence
  };

  const actual = explode(create().invert());

  assert.deepStrictEqual(actual, expected);
});

test("Permutation_ReverseTwice_BackToOriginal", () => {
  const actual = explode(create().reverse().reverse());

  assert.deepStrictEqual(actual, example);
});

test("Permutation_ReflectTwice_BackToOriginal", () => {
  const actual = explode(create().reflect().reflect());

  assert.deepStrictEqual(actual, example);
});

test("Permutation_InvertTwice_BackToOriginal", () => {
  const actual = explode(create().invert().invert());

  assert.deepStrictEqual(actual, example);
});

test("Permutation_WithOffsetWithGarbage_Throws", () => {
  const instance = create();

  testIntParamPreconditions(instance.withOffset, -1, BASE);
});

test("Permutation_WithOffset_AdjustsSequence", () => {
  const offset = 7;

  const example = {
    base: BASE,
    number: 18258836,
    offset: 7,
    sequence: [7, 1, 8, 11, 5, 4, 0, 6, 3, 9, 2, 10],
  };

  const actual = explode(create().withOffset(7));

  assert.deepStrictEqual(actual, example);
});

test("Permutation_EqualsVarious_ReturnExpected", () => {
  const a = create();
  const b = create();

  testEquals(a, b, a.withOffset(7));
});
