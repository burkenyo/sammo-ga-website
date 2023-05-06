// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

import { assert, lazy } from "@shared/utils";
import { ApiError, ApiErrorCause } from "./apiRunner";
import { Fractional, OeisFractionalExpansion, OeisId } from "@melodies/oeis";

type Stored<T extends { id: OeisId }> = { className: string } & Omit<T, "id">;

const CLASS_NAMES: ReadonlyMap<string, string> = new Map([
  [ApiError.name, "ApiError"],
  [OeisFractionalExpansion.name, "OeisFractionExpansion"],
]);

export class ExpansionsDb {
  #db = lazy(() => {
    const request = indexedDB.open("DozenalExpansionsDB");

    return new Promise<IDBDatabase>((resolve, reject) => {
      request.onerror = (e) => {
        console.log("Error opening db!", e);
        reject(new Error("Error opening db!"));
      };

      request.onsuccess = (e) => {
        resolve((e.target as IDBOpenDBRequest).result);
      };

      request.onupgradeneeded = (e) => {
        console.log("Creating db...");
        const db = (e.target as IDBOpenDBRequest).result;
        db.createObjectStore("DozenalExpansions");
      };
    });
  });

  async getFromDb(id: OeisId): Promise<Optional<Either<ApiError, OeisFractionalExpansion>>> {
    assert(id instanceof OeisId, "Unexpected type: ", typeof id);

    const db = await this.#db.value;
    const xact = db.transaction("DozenalExpansions", "readwrite");
    const store = xact.objectStore("DozenalExpansions");
    let request = store.get(String(id));

    return new Promise((resolve, reject) => {
      request.onerror = (e) => {
        xact.abort();

        console.log("Error executing get!", e);
        reject(new Error("Error executing get!"));
      };

      request.onsuccess = (e) => {
        const result = (e.target as IDBRequest).result as Optional<Stored<OeisFractionalExpansion | ApiError>>;

        if (!result) {
          resolve(null);
          return;
        }

        // hydrate into one of our classes
        switch (result.className) {
          case CLASS_NAMES.get(ApiError.name): {
            const stored = result as Stored<ApiError>;

            if (typeof stored.message != "string" || typeof stored.name != "string"
              || !Object.values(ApiErrorCause).includes(stored.cause)
            ) {
              break;
            }

            xact.commit();
            const error = new ApiError(stored.message, stored.cause, id);

            resolve({ left: error });
            return;
          }
          case CLASS_NAMES.get(OeisFractionalExpansion.name): {
            const stored = result as Stored<OeisFractionalExpansion>;

            if (typeof stored.name != "string" || typeof stored.expansion?.radix != "number"
              || typeof stored.expansion?.offset != "number" || !(stored.expansion?.digits instanceof Uint8Array)
            ) {
              break;
            }

            let fractional: Fractional;

            try {
              fractional = Fractional.create(stored.expansion.radix, stored.expansion.offset, stored.expansion.digits);
            } catch {
              break;
            }

            xact.commit();
            const expansion = new OeisFractionalExpansion(id, result.name, fractional);

            resolve({ right: expansion });
            return;
          }
        }

        // item was found but could not be hydrated, so remove it
        console.warn(`Unable to hydrate store value for ${id}! Purging from db...`);
        request = store.delete(String(id));

        request.onerror = (e) => {
          xact.abort();

          console.log("Error executing delete!", e);
          reject(new Error("Error executing delete!"));
        };

        request.onsuccess = () => {
          xact.commit();

          resolve(null);
        };
      };
    });
  }

  async addToDb(expansionOrError: OeisFractionalExpansion | ApiError): Promise<void> {
    const db = await this.#db.value;
    const xact = db.transaction("DozenalExpansions", "readwrite");
    const store = xact.objectStore("DozenalExpansions");

    const { id, ...rest } = expansionOrError;

    const valueToStore: Stored<OeisFractionalExpansion | ApiError> = {
      className: CLASS_NAMES.get(expansionOrError.constructor.name)!,
      ...rest,
    };

    const request = store.put(valueToStore, String(id));

    return new Promise<void>((resolve, reject) => {
      request.onerror = (e) => {
        xact.abort();

        console.log("Error executing put!", e);
        reject(new Error("Error executing put!"));
      };

      request.onsuccess = () => {
        xact.commit();
        resolve();
      };
    });
  }
}
