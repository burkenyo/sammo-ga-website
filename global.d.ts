type Nullable<T extends Object> = T | null | undefined;

type TypedArrayMutableProperties = 'copyWithin' | 'fill' | 'reverse' | 'set' | 'sort' | 'buffer';

interface ReadonlyUint8Array extends Omit<Uint8Array, TypedArrayMutableProperties> {
  readonly [index: number]: number;
}

interface Equatable {
  equals: (other: unknown) => boolean;
}
