// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

import { validateConstructorKey } from "@shared/utils";

export class OeisId implements Equatable {
  static readonly MAX_VALUE = 999_999_999;

  readonly value: number;

  constructor(value: number) {
    if (!Number.isInteger(value)) {
      throw new TypeError("value");
    }

    if (value < 1 || value > OeisId.MAX_VALUE) {
      throw new RangeError("value");
    }

    this.value = value;
  }

  toString(): string {
    return "A" + this.value.toFixed().padStart(6, "0");
  }

  static parse(value: string): OeisId {
    // Parse out the number exactly and let the constructor reject it if it’s not an integer.
    // This is to avoid the implicit rounding that Number.parseInt() does.
    // It also prevents implicit parsing of other bases.
    const floatValue = value[0] == "A"
      ? Number.parseFloat(value.substring(1))
      : Number.parseFloat(value);

    return new OeisId(floatValue);
  }

  equals(other: unknown): boolean {
    return other instanceof OeisId && this.value == other.value;
  }
}

// currently, this class only supports dozenal (base-12)
export class Fractional {
  static readonly #CONSTRUCTOR_KEY = Symbol();

  static readonly DOZENAL_DIGIT_MAP = "0123456789XE";

  readonly radix: number;
  readonly offset: number;
  readonly digits: ReadonlyUint8Array;

  private constructor(key: symbol, radix: number, offset: number, digits: ReadonlyUint8Array) {
    validateConstructorKey(key, Fractional.#CONSTRUCTOR_KEY, Fractional);

    this.radix = radix;
    this.offset = offset;
    this.digits = digits;
  }

  static create(radix: number, offset: number, digits: ReadonlyUint8Array) {
    if (radix != 12) {
      throw TypeError("radix");
    }

    if (offset < 0 || offset > digits.length) {
      throw RangeError("offset");
    }

    for (const digit of digits) {
      if (digit < 0 || digit >= radix) {
        throw new TypeError("digits");
      }
    }

    return new Fractional(this.#CONSTRUCTOR_KEY, radix, offset, digits);
  }

  static parseDozenal(dozenal: string): Fractional {
    let skip = 0;

    while (dozenal[skip] == "0") {
      skip++;
    }

    const offset = dozenal.indexOf(";") == -1
      ? dozenal.length - skip
      : dozenal.indexOf(";") - skip;

    const getDigits = (function* () {
      for (let i = skip; i < dozenal.length; i++) {
        if (dozenal[i] == ";") {
          continue;
        }

        const digit = Fractional.DOZENAL_DIGIT_MAP.indexOf(dozenal[i]);

        if (digit == -1) {
          throw new RangeError(`Invalid digit! ${dozenal[i]}`);
        }

        yield digit;
      }
    })();

    return new Fractional(this.#CONSTRUCTOR_KEY, 12, offset, new Uint8Array(getDigits));
  }

  toString(): string {
    const chars = [];

    if (this.offset == 0) {
      chars.push("0");
      chars.push(";");
    }

    let i = 0;
    for (const digit of this.digits) {
      if (this.offset > 0 && i == this.offset) {
        chars.push(";");
      }

      chars.push(Fractional.DOZENAL_DIGIT_MAP[digit]);

      i++;
    }

    return chars.join("");
  }
}

export class OeisFractionalExpansion {
  readonly id: OeisId;
  readonly name: string;
  readonly expansion: Fractional;

  constructor(id: OeisId, name: string, expansion: Fractional) {
    this.id = id;
    this.name = name;
    this.expansion = expansion;
  }

  static parseRawText(text: string): OeisFractionalExpansion {
    const lines = text.split(/\r?\n/g, 3);

    const id = OeisId.parse(lines[0]);
    const name = lines[1];
    const expansion = Fractional.parseDozenal(lines[2]);

    return new OeisFractionalExpansion(id, name, expansion);
  }
}
