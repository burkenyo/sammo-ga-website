// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

export class CsvWriter {
  static readonly #CHUNK_SIZE = 1024;
  static readonly #ENCODER = new TextEncoder();

  readonly #fields: readonly string[];
  readonly #lastIndex: number;
  #currentChunk = new Uint8Array(CsvWriter.#CHUNK_SIZE);
  #position = 0;
  readonly #chunks = [this.#currentChunk];

  constructor(fields: readonly string[]) {
    if (!fields || !fields.length) {
      throw new TypeError("No fields were provided!");
    }

    this.#fields = [...fields];
    this.#lastIndex = fields.length - 1;

    for (let i = 0; i < this.#lastIndex; i++) {
      this.#writeString(this.#fields[i]);
      this.#writeRaw(",");
    }

    this.#writeString(this.#fields[this.#lastIndex]);
    this.#writeRaw("\r\n");
  }

  #writeString(value: string) {
    if (value.includes("\"")) {
      this.#writeRaw("\"");
      this.#writeRaw(value.replace(/"/g, "\"\""));
      this.#writeRaw("\"");
    } else if (value.includes(",")) {
      this.#writeRaw("\"");
      this.#writeRaw(value);
      this.#writeRaw("\"");
    } else {
      this.#writeRaw(value);
    }
  }

  #writeRaw(value: string) {
    const { read, written } = CsvWriter.#ENCODER.encodeInto(value, this.#currentChunk.subarray(this.#position));
    this.#position += written!;

    if (this.#position == CsvWriter.#CHUNK_SIZE) {
      this.#currentChunk = new Uint8Array(CsvWriter.#CHUNK_SIZE);
      this.#position = 0;
      this.#chunks.push(this.#currentChunk);
    }

    if (read! < value.length) {
      this.#writeRaw(value.slice(read));
    }
  }

  writeRow(values: { [key: string]: (string | number | null | bigint | boolean) }): void {
    if (!values) {
      values = {};
    }

    for (let i = 0; i < this.#lastIndex; i++) {
      const value = values[this.#fields[i]];

      if (value != null) {
        this.#writeValue(value);
      }

      this.#writeRaw(",");
    }

    const lastValue = values[this.#fields[this.#lastIndex]];

    if (lastValue != null) {
      this.#writeValue(lastValue);
    }

    this.#writeRaw("\r\n");
  }

  #writeValue(value: string | number | null | bigint | boolean) {
    if (typeof value == "string") {
      this.#writeString(value);
    } else if (value != null) {
      this.#writeRaw(String(value));
    }
  }

  getBlob(): Blob {
    // Use a trimmed version of the last chunk for the blob.
    this.#chunks[this.#chunks.length - 1] = this.#currentChunk.subarray(0, this.#position);

    const blob = new Blob(this.#chunks);

    // Reset the last chunk in case more writing is to be performed.
    this.#chunks[this.#chunks.length - 1] = this.#currentChunk;

    return blob;
  }
}
