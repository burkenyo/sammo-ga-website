import { test, assert } from "vitest";
import * as utils from "@/utils";

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
