import { lazy } from "@/utils";
import { ApiError } from "./apiRunner";
import { Fractional, OeisFractionalExpansion, type OeisId } from "@/oeis";

type Stored<T extends { id: OeisId }> = { className: string } & Omit<T, "id">;

export class ExpansionsDb {
  #getDb = lazy(() => {
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
        console.log("creating db");
        const db = (e.target as IDBOpenDBRequest).result;
        db.createObjectStore("DozenalExpansions");
      };
    });
  });

  async getFromDb(id: OeisId): Promise<Optional<Either<ApiError, OeisFractionalExpansion>>> {
    const db = await this.#getDb();
    const xact = db.transaction("DozenalExpansions");
    const store = xact.objectStore("DozenalExpansions");
    const request = store.get(String(id));

    return new Promise((resolve, reject) => {
      request.onerror = (e) => {
        xact.abort();

        console.log("Error executing get!", e);
        reject(new Error("Error executing get!"));
      };

      request.onsuccess = (e) => {
        xact.commit();

        const result = (e.target as IDBRequest).result as Optional<Stored<OeisFractionalExpansion | ApiError>>;

        if (!result) {
          resolve(null);
          return;
        }

        // hydrate into one of our classes
        switch (result.className) {
          case ApiError.name: {
            const stored = result as Stored<ApiError>;
            const error = new ApiError(stored.message, stored.cause, id);

            resolve({ left: error });
            return;
          }
          case OeisFractionalExpansion.name: {
            const stored = result as Stored<OeisFractionalExpansion>;
            const expansion = new OeisFractionalExpansion(
              id,
              result.name,
              Fractional.create(stored.expansion.radix, stored.expansion.offset, stored.expansion.digits)
            );

            resolve({ right: expansion });
            return;
          }
        }

        reject(new TypeError(`Unexpected className: ${result.className}!`));
      };
    });
  }

  async addToDb(expansionOrError: OeisFractionalExpansion | ApiError): Promise<void> {
    const db = await this.#getDb();
    const xact = db.transaction("DozenalExpansions", "readwrite");
    const store = xact.objectStore("DozenalExpansions");

    const { id, ...rest } = expansionOrError;

    const valueToStore: Stored<OeisFractionalExpansion | ApiError> = {
      className: expansionOrError.constructor.name,
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
