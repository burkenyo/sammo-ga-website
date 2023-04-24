// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

import { test, assert } from "vitest";
import * as utils from "@shared/utils";

test("utils_assertWithFalsy_throws", () => {
  try {
    utils.assert(false, "Assert failure! ", 1);

    assert.fail("Expected an error to be thrown!");
  } catch (error: any) {
    assert.strictEqual(error.message, "Assert failure! 1");
  }

  try {
    utils.assert(0, "Assert failure!");

    assert.fail("Expected an error to be thrown!");
  } catch (error: any) {
    assert.strictEqual(error.message, "Assert failure!");
  }
});

test("utils_assertWithTruthy_ok", () => {
  utils.assert(true, "all good!");
});

test("utils_requireWithFalsy_throws", () => {
  try {
    utils.requireTruthy(false, "Assert failure! ", 1);

    assert.fail("Expected an error to be thrown!");
  } catch (error: any) {
    assert.strictEqual(error.message, "Assert failure! 1");
  }

  try {
    utils.requireTruthy(0, "Assert failure!");

    assert.fail("Expected an error to be thrown!");
  } catch (error: any) {
    assert.strictEqual(error.message, "Assert failure!");
  }
});

test("utils_requireWithTruthy_ok", () => {
  const expected = Symbol();

  assert.strictEqual(utils.requireTruthy(expected, "all good!"), expected);
});

test("utils_isTrue_returnsAsExpected", () => {
  function testIsTrue(value: Optional<boolean | string | number | bigint>, expected: boolean) {
    assert.strictEqual(utils.isTrue(value), expected);
  }

  // bools
  testIsTrue(false, false);
  testIsTrue(true, true);

  // strings
  testIsTrue("", false);
  testIsTrue("false", false);
  testIsTrue("true", true);
  testIsTrue("True", true);

  // numeric
  testIsTrue(-1, true);
  testIsTrue(0, false);
  testIsTrue(NaN, false);
  testIsTrue(1, true);
  testIsTrue(BigInt(-1), true);
  testIsTrue(BigInt(0), false);
  testIsTrue(BigInt(1), true);

  // null-ike
  testIsTrue(null, false);
  testIsTrue(undefined, false);
});

test("utils_lazy_evaluatesLazily", () => {
  let buildCount = 0;

  const expected = Symbol();

  const lazy = utils.lazy(() => {
    buildCount++;

    return expected;
  });

  assert.strictEqual(buildCount, 0);

  assert.strictEqual(lazy(), expected);

  assert.strictEqual(buildCount, 1);

  assert.strictEqual(lazy(), expected);

  assert.strictEqual(buildCount, 1);
});

class Foo {
  static readonly #CONSTRUCTOR_KEY = Symbol();

  // not marking private so the test can access it
  constructor(key: symbol) {
    utils.validateConstructorKey(key, Foo.#CONSTRUCTOR_KEY, Foo);
  }

  static create(): Foo {
    return new Foo(this.#CONSTRUCTOR_KEY);
  }
}

test("utils_validateConstructorKeyCalledExternally_throws", () => {
  assert.throws(() => new Foo(Symbol()));
});

test("utils_validateConstructorKeyCalledInternally_ok", () => {
  Foo.create();
});

const PROMISE_VALUE = 47;

function getPromise(): { promise: Promise<number>; completer: () => void } {
  let resolveLocal: (val: typeof PROMISE_VALUE) => void;

  const promise = new Promise<typeof PROMISE_VALUE>(resolve => resolveLocal = resolve);

  return { promise, completer: () => resolveLocal(PROMISE_VALUE) };
}

async function dontWaitForever<T extends {}>(
  promise: Promise<Optional<T>>, shouldFailIfNotResolved: boolean
): Promise<Optional<T>> {
  const guard = (async () => {
    // skip two event ticks
    for (let i = 0; i < 2; i++) {
      await new Promise<void>(resolve => setTimeout(() => resolve(), 0))
    }

    assert.isFalse(shouldFailIfNotResolved, "Cancellable promise was not resolved!");
  })();

  const result = await Promise.race([promise, guard]) as Promise<Optional<T>>;

  shouldFailIfNotResolved = false;

  return result;
}

test("Delay_awaited_resolves", async () => {
  await dontWaitForever(utils.delay(0), true);
});

test("CancellablePromise_noCancel_isNotCanceled", async () => {
  const cancellable = new utils.CancellablePromise();

  await dontWaitForever(cancellable, false);

  assert.isFalse(cancellable.isCanceled);
});

test("CancellablePromise_cancel_isCanceled", async () => {
  const cancellable = new utils.CancellablePromise();

  cancellable.cancel();

  await dontWaitForever(cancellable, true);

  assert.isTrue(cancellable.isCanceled);
});

test("CancellablePromise_noCancel_hasValueWhenNotCanceled", async () => {
  const { promise, completer } = getPromise();

  const cancellable = new utils.CancellablePromise(promise);

  completer();

  const result = await dontWaitForever(cancellable, true);

  assert.isFalse(cancellable.isCanceled);
  assert.strictEqual(result, PROMISE_VALUE);
});

test("CancellablePromise_cancel_hasNoValueWhenCanceled", async () => {
  const { promise } = getPromise();

  const cancellable = new utils.CancellablePromise(promise);

  cancellable.cancel();

  const result = await dontWaitForever(cancellable, true);

  assert.isTrue(cancellable.isCanceled);
  assert.include([null, undefined], result);
});

test("utils_timeDiff_correctDifferenceCalculated", () => {
  const millis = Date.parse("1984-06-03T08:20:13.047-06:00");

  const first = new Date(millis);

  // 6 hours, 22.5 minutes (6.375 hours)
  // this is a number that when divided by the number of milliseconds in a day
  // produces no floating-point rounding error
  const later = new Date(millis + 382.5 * 60 * 1000);

  assert.strictEqual(utils.timeDiff(later, first, utils.TimeUnit.Millisecond), 382.5 * 60 * 1000);
  assert.strictEqual(utils.timeDiff(first, later, utils.TimeUnit.Second), -382.5 * 60);
  assert.strictEqual(utils.timeDiff(later, first, utils.TimeUnit.Minute), 382.5);
  assert.strictEqual(utils.timeDiff(first, later, utils.TimeUnit.Hour), -6.375);
  assert.strictEqual(utils.timeDiff(later, first, utils.TimeUnit.Day), 0.265625);
});
