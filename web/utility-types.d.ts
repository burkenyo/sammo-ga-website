// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

type Optional<T extends {}> = T | null | undefined;

type TypedArrayMutableProperties = "copyWithin" | "fill" | "reverse" | "set" | "sort" | "buffer";

interface ReadonlyUint8Array extends Omit<Uint8Array, TypedArrayMutableProperties> {
  readonly [index: number]: number;
}

interface Equatable {
  equals: (other: unknown) => boolean;
}

type Either<E extends Error, T extends {}> =
{
  left: E,
  right?: never
} | {
  left?: never,
  right: T
  }

type JsonScalar = null | string | number | boolean;
// note that Json, JsonArray, and JsonObject have a recursive relationship to each other
type Json = JsonScalar | JsonArray | JsonObject;
type JsonArray = Json[];
type JsonObject = { [key: string]: Json };

// override the provided declaration of JSON.parse to return a known type
interface JSON {
  parse(text: string, reviver?: ((this: any, key: string, value: any) => any) | undefined) : Json
}
