// Theory behind converting permutation numbers to sequences:
//   http://en.wikipedia.org/wiki/Factorial_number_system
//   https://en.wikipedia.org/wiki/Lehmer_code

namespace Helpers {
  // the largest base for which maxPermutation <= MAX_SAFE_INTEGER
  export const MAX_BASE = 19;

  // Compute the factorials of 1 through MAX_BASE - 1.
  // Store them in reverse order for easier indexing later.
  const factorials: readonly number[] = (() => {
    const builder = [];
    let previous = 1;
    for (let i = 1; i <= MAX_BASE - 1; i++) {
      const current = previous * i;
      builder[MAX_BASE - i - 1] = current;
      previous = current;
    }

    return builder;
  })();

  // Compute an array of numbers 1 through MAX_BASE - 1.
  // Don’t include zero because the first item in a permutation is implicitly zero.
  const allDigits: readonly number[] = (() => {
    const builder = [];
    for (let i = 1; i <= MAX_BASE - 1; i++) {
      builder.push(i);
    }

    return builder;
  })();

  export function getDigitBag(base: number) {
    return allDigits.slice(0, base - 1);
  }

  export function getFactorials(base: number) {
    return factorials.slice(MAX_BASE - base + 1);
  }

  export function getMaxPermutationNumber(base: number) {
    return factorials[MAX_BASE - base];
  }
}

export class Permutation {
  static readonly MAX_BASE = Helpers.MAX_BASE;

  readonly number: number;
  readonly sequence: readonly number[];

  public get base(): number {
    return this.sequence.length;
  }

  public get offset(): number {
    return this.sequence[0];
  }

  private constructor(number: number, sequence: readonly number[]) {
    this.number = number;
    this.sequence = sequence;
  }

  private static validateBase(base: number) {
    if (!Number.isInteger(base)) {
      throw new TypeError("base");
    }

    if (base < 2 || base > Helpers.MAX_BASE) {
      throw new RangeError("base");
    }
  }

  private static validateOffset(base: number, offset: number) {
    if (!Number.isInteger(offset)) {
      throw new TypeError("offset");
    }

    if (offset < 0 || offset > base - 1) {
      throw new RangeError("offset");
    }
  }

  static create(base: number, number: number, offset: number): Permutation {
    this.validateBase(base);
    this.validateOffset(base, offset);

    if (!Number.isInteger(number)) {
      throw new TypeError("number");
    }

    if (number < 1 || number > Helpers.getMaxPermutationNumber(base)) {
      throw new RangeError("number");
    }

    return this._create(base, number, offset);
  }

  static createRandom(base: number): Permutation {
    this.validateBase(base);

    const number = Math.floor(Math.random() * Helpers.getMaxPermutationNumber(base)) + 1;
    const offset = Math.floor(Math.random() * base);

    return this._create(base, number, offset);
  }

  private static _create(base: number, number: number, offset: number): Permutation {
    const factorials = Helpers.getFactorials(base);
    // This is a “zero-based” Lehmer code.
    // It is used to pull digits out of the digitBag.
    const lehmer = [];

    let mods = number - 1;

    for (let i = 0; i < base - 2; i++) {
      lehmer[i] = Math.floor(mods/factorials[i]);
      mods = mods % factorials[i];
    }

    const digitBag = Helpers.getDigitBag(base);

    // build the sequence by pulling digits out of the digit bag,
    // adding the transposition, and wrapping-around by the base

    // the first element in the sequence is simply the transposition
    const sequence = [offset];

    for (let i = 0; i < base - 2; i++) {
      sequence.push((digitBag[lehmer[i]] + offset) % base);

      // This digit is “used up”; remove it.
      digitBag.splice(lehmer[i], 1);
    }
    // the remaining digit goes last in the sequence
    sequence.push((digitBag[0] + offset) % base);

    return new Permutation(number, sequence);
  }

  static fromSequence(sequence: readonly number[]): Permutation {
    if (new Set(sequence).size != sequence.length) {
      throw new TypeError("sequence");
    }

    const base = sequence.length;
    this.validateBase(base);

    if (Math.min(...sequence) != 0 || Math.max(...sequence) != base - 1) {
      throw new TypeError("sequence");
    }

    return this._fromSequence(sequence);
  }

  private static _fromSequence(sequence: readonly number[]): Permutation {
    const base = sequence.length;
    const factorials = Helpers.getFactorials(base);
    const digitBag = Helpers.getDigitBag(base);
    let number = 1;
    const offset = sequence[0];

    for (let i = 0; i < base - 2; i++) {
      const index = digitBag.indexOf((base + sequence[i + 1] - offset) % base);
      number += index * factorials[i];
      digitBag.splice(index, 1);
    }

    return new Permutation(number, sequence);
  }

  static getMaxNumber(base: number) {
    this.validateBase(base);

    return Helpers.getMaxPermutationNumber(base);
  }

  reverse(): Permutation {
    const reversed = [...this.sequence].reverse();

    return Permutation._fromSequence(reversed);
  }

  reflect(): Permutation {
    // The permutation number of the flipped sequence is simply the max number - the current number.
    // Add one because permutation numbers start at 1, not 0
    const number = Helpers.getMaxPermutationNumber(this.base) + 1 - this.number;

    // Invert the sequence by subtracting each element from base.
    // Note that 0 is special-cased because it would wrap-around, i.e. (base - 0) % base = 0.
    const flipped = this.sequence.map(e => e == 0 ? 0 : this.base - e);

    return new Permutation(number, flipped);
  }

  invert(): Permutation {
    const inverted = [];
    for (let i = 0; i < this.base; i++) {
      // when computing the inverse of an element, add the offset back
      inverted[this.sequence[i]] = i;
    }

    return Permutation.fromSequence(inverted);
  }

  withOffset(offset: number) {
    Permutation.validateOffset(this.base, offset);

    // “bias” the delta by base so that it is known to never be negative
    const delta = this.base + offset - this.offset;

    const offsetApplied = this.sequence.map(e => (e + delta) % this.base);

    // The permutation number does not change when only modifying the offset.
    return new Permutation(this.number, offsetApplied);
  }
}
