// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

import exampleElection from "@vote-demo/example-election.json";
import { lazy } from "@shared/utils";

export interface Election {
  readonly nominations: readonly string[];
  readonly ballots: readonly (readonly string[])[];
  logBallot(key: string, preferences: readonly string[], force: boolean): boolean;
  reset(): void
  clear(): void;
}

interface BallotsAsObject {
  readonly [key: string]: readonly string[];
}

namespace LocalStorageHelper {
  const STORAGE_KEY = "Ballots";

  // eslint-disable-next-line no-inner-declarations
  function isObjectOfBallots(value: Json): value is { [key: string]: string[] } {
    if (!value || typeof value != "object" || Array.isArray(value)) {
      return false;
    }

    for (const prop of Object.values(value)) {
      if (!Array.isArray(prop)) {
        return false;
      }

      for (const item of prop) {
        if (typeof item != "string") {
          return false;
        }
      }
    }

    return true;
  }

  export function reset(): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(null));
  }

  export function load(): BallotsAsObject | undefined {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (!stored) {
      reset();
      return;
    }

    const parsed = JSON.parse(stored)
    if (!isObjectOfBallots(parsed)) {
      reset();
      return;
    }

    return parsed;
  }

  export function write(data: BallotsAsObject): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
  }
}

class LocalStorageElection implements Election {
  readonly nominations: readonly string[];
  readonly #ballots: Map<string, readonly string[]> = new Map();

  #loadBallots(data: BallotsAsObject): void {
    this.#ballots.clear();

    for (const [id, ballot] of Object.entries(data)) {
      this.#ballots.set(id, [...ballot]);
    }
  }

  get ballots(): readonly (readonly string[])[] {
    return [...this.#ballots.values()];
  }

  constructor() {
    this.nominations = [...exampleElection.nominations].sort();

    const stored = LocalStorageHelper.load();
    if (stored) {
      this.#loadBallots(stored);
      return
    }

    this.#loadBallots(exampleElection.ballots);
  }

  logBallot(key: string, preferences: readonly string[], force: boolean): boolean {
    if (this.#ballots.has(key) && !force) {
      return false;
    }

    this.#ballots.set(key, [...preferences]);
    LocalStorageHelper.write(Object.fromEntries(this.#ballots.entries()));

    return true;
  }

  reset(): void {
    this.#loadBallots(exampleElection.ballots);
    LocalStorageHelper.reset();
  }

  clear(): void {
    this.#ballots.clear();
    LocalStorageHelper.write({});
  }
}

const election = lazy(() => new LocalStorageElection());

export function useElection(): Election {
  return election.value;
}
