export class AssertError extends Error {}

export function assert<T>(value: T, message: string, param?: any) {
  if (!value) {
    if (param) {
      message += param;
    }

    throw new AssertError(message);
  }
}

export function require<T>(value: T, message: string, param?: any): NonNullable<T> {
  if (!value) {
    if (param) {
      message += param;
    }

    throw new TypeError(message);
  }

  return value as NonNullable<T>;
}

export function isTrue(value: boolean | string | number | bigint | undefined | null): boolean {
  if (typeof value == "string") {
    return value.toLowerCase() == "true";
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

type TypedArrayMutableProperties = 'copyWithin' | 'fill' | 'reverse' | 'set' | 'sort' | 'buffer';

export interface ReadonlyUint8Array extends Omit<Uint8Array, TypedArrayMutableProperties> {
  readonly [index: number]: number;
}
