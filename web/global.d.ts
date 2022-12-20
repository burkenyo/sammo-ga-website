type Optional<T extends {}> = T | null | undefined;

type TypedArrayMutableProperties = 'copyWithin' | 'fill' | 'reverse' | 'set' | 'sort' | 'buffer';

interface ReadonlyUint8Array extends Omit<Uint8Array, TypedArrayMutableProperties> {
  readonly [index: number]: number;
}

interface Equatable {
  equals: (other: unknown) => boolean;
}

type Either<E extends Error, T extends {}> = {
  left: E,
  right?: never
} | {
  left?: never,
  right: T
}

declare module "vue-spinner/src/RingLoader.vue";
