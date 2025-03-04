// Copyright © 2025 Samuel Justin Speth Gabay
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

// Override the provided declaration of JSON.parse to return a known type.
// This requires omitting the reviver parameter, which could create arbitrary objects.
interface JSON {
  parse(text: string) : Json
}
