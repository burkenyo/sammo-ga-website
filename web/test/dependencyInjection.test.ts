// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

import { test, assert } from "vitest";
import { ServiceLifetime, serviceKey, dependencies, ContainerBuilder } from "@shared/dependencyInjection";

class A {

}

class B {
  static readonly [dependencies] = ["A"] as const;

  readonly a: A;

  constructor(a: A) {
    this.a = a;
  }
}

class C {
  readonly id: Symbol;
  readonly b: B;

  constructor(id: Symbol, b: B) {
    this.id = id;
    this.b = b;
  }
}

class D {
  static readonly [dependencies] = ["C"] as const;

  readonly c: C;

  constructor(c: C) {
    this.c = c;
  }
}

const serviceKeys = {
  A: serviceKey<A>("A"),
  B: serviceKey<B>("B"),
  C: serviceKey<C>("C"),
  D: serviceKey<D>("D"),
} as const;

const idForC = Symbol("id");

const fullContainer = new ContainerBuilder()
  .registerInjected(ServiceLifetime.Singleton, serviceKeys.B, B)
  .registerSingletonInstance(serviceKeys.A, new A())
  .registerFactory(ServiceLifetime.Transient, serviceKeys.C, provide => new C(idForC, provide(serviceKeys.B)))
  .registerInjected(ServiceLifetime.Transient, serviceKeys.D, D)
  .build();

test("depependencyInjection_incompleteRegistration_throws", () => {
  const container = new ContainerBuilder()
    .registerInjected(ServiceLifetime.Singleton, serviceKeys.B, B)
    .build();

  assert.throws(() => container.retrieve(serviceKeys.A), /no service was registered/i);
  assert.throws(() => container.retrieve(serviceKeys.B), /no service key found/i);
});

test("depependencyInjection_completeRegistration_ok", () => {
  fullContainer.retrieve(serviceKeys.D);
});

test("depependencyInjection_retrieveSingleton_sameInstanceAlwaysReturned", () => {
  const b1 = fullContainer.retrieve(serviceKeys.B);
  const b2 = fullContainer.retrieve(serviceKeys.B);

  assert.strictEqual(b1, b2);
});

test("depependencyInjection_retrieveTransient_newInstanceAlwaysReturned", () => {
  const d1 = fullContainer.retrieve(serviceKeys.D);
  const d2 = fullContainer.retrieve(serviceKeys.D);

  assert.notStrictEqual(d1, d2);
});

test("dependencyInjection_retrieveFromFactory_factoryExecuted", () => {
  const c = fullContainer.retrieve(serviceKeys.C);

  assert.strictEqual(c.id, idForC);
});

test("dependencyInjection_retrieveFromFactory_factoryExecutedLazily", () => {
  class X { }

  let buildCount = 0;
  const xKey = serviceKey<X>("X");
  const container = new ContainerBuilder()
    .registerFactory(ServiceLifetime.Singleton, xKey, () => {
      buildCount++;

      return new X();
    })
    .build();

  assert.strictEqual(buildCount, 0);

  container.retrieve(xKey);

  assert.strictEqual(buildCount, 1);

  container.retrieve(xKey);

  assert.strictEqual(buildCount, 1);
});

test("dependencyInjection_cyclicDependency_throws", () => {
  class X {
    static readonly [dependencies] = ["Y"] as const;

    constructor(y: Y) { }
  }

  class Y {
    static readonly [dependencies] = ["X"] as const;

    constructor(x: X) { }
  }

  const xKey = serviceKey<X>("X");
  const yKey = serviceKey<Y>("Y");

  const container = new ContainerBuilder()
    .registerInjected(ServiceLifetime.Singleton, xKey, X)
    .registerInjected(ServiceLifetime.Singleton, yKey, Y)
    .build();

  assert.throws(() => container.retrieve(xKey), /cyclic dependency detected/i);
  assert.throws(() => container.retrieve(yKey), /cyclic dependency detected/i);
});

test("dependencyInjection_depenencyCountMismatch_throws", () => {
  class X { }

  class Y {
    static readonly [dependencies] = ["X"] as const;
  }

  const yKey = serviceKey<Y>("X");

  assert.throws(() =>
    new ContainerBuilder().registerInjected(ServiceLifetime.Singleton, yKey, Y), /dependency count mismatch/i);
});

test("dependencyInjection_singletonRequiresTransient_throws", () => {
  class X { }

  class Y {
    static readonly [dependencies] = ["X"] as const;

    constructor(x: X) { }
  }

  const xKey = serviceKey<X>("X");
  const yKey = serviceKey<Y>("Y");

  const container = new ContainerBuilder()
    .registerInjected(ServiceLifetime.Transient, xKey, X)
    .registerInjected(ServiceLifetime.Singleton, yKey, Y)
    .build();

  assert.throws(() => container.retrieve(yKey), /cannot require.*longer lifetime/i);
});
