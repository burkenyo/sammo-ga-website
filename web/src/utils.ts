export class AssertError extends Error { }

export function assert<T>(value: T, message: string, param?: unknown) {
  if (!value) {
    if (param) {
      message += param;
    }

    throw new AssertError(message);
  }
}

export function requireTruthy<T>(value: T, message: string, param?: unknown): NonNullable<T> {
  if (!value) {
    if (param) {
      message += param;
    }

    throw new TypeError(message);
  }

  return value as NonNullable<T>;
}

export const FALSE_STRING = String(false);
export const TRUE_STRING = String(true);

export function isTrue(value: Optional<boolean | string | number | bigint>): boolean {
  if (typeof value == "string") {
    return value.toLowerCase() == TRUE_STRING;
  }

  return !!value;
}

export function lazy<T extends {}>(factory: () => T): () => T {
  let value: T;

  function get(): T {
    if (value) {
      return value;
    }

    value = factory();

    return value;
  }

  return get;
}

export function delay(millis: number): Promise<void> {
  return new Promise(resolve => {
    setTimeout(() => resolve(), millis);
  });
}

export class CancellablePromise<T extends {}> implements Promise<Optional<T>> {
  readonly #promise: Promise<Optional<T>>;
  #isCanceled = false;
  #resolve!: () => void;

  get isCanceled(): boolean {
    return this.#isCanceled;
  }

  get [Symbol.toStringTag]() {
    return "CancellablePromise";
  }

  constructor(promise?: Optional<Promise<T>>) {
    const canceler = new Promise<void>(resolve => { this.#resolve = resolve });

    this.#promise = (
      promise
        ? Promise.race([promise, canceler])
        : canceler
     ) as Promise<Optional<T>>
  }

  then<TResult1 = Optional<T>, TResult2 = never>(onfulfilled?: ((value: Optional<T>) => TResult1 | PromiseLike<TResult1>) | null | undefined, onrejected?: ((reason: unknown) => TResult2 | PromiseLike<TResult2>) | null | undefined): Promise<TResult1 | TResult2> {
    return this.#promise.then(onfulfilled, onrejected);
  }

  catch<TResult = never>(onrejected?: ((reason: unknown) => TResult | PromiseLike<TResult>) | null | undefined): Promise<Optional<T> | TResult> {
    return this.#promise.then(onrejected);
  }

  finally(onfinally?: (() => void) | null | undefined): Promise<Optional<T>> {
    return this.#promise.finally(onfinally);
  }

  cancel() {
    this.#resolve();
    this.#isCanceled = true;
  }
}

// Here clazz is typed as Function rather than new (...args: unknown[]) => object
// because TypeScript won’t allow me to assign a private constructor to the latter definition
export function validateConstructorKey(providedKey: symbol, constructorKey: symbol, clazz: Function) {
  if (providedKey != constructorKey) {
    throw new TypeError(
      clazz.name + " constructor is private! It cannot be invoked outside of the class definition.")
  }
}
