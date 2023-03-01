// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

import { OeisFractionalExpansion, OeisId } from "@/oeis";
import type { ExpansionsDb } from "./expansionsDb";
import { timeDiff, TimeUnit } from "@/utils";

export enum ApiErrorCause {
  NotFound = "NotFound",
  InvalidSequence = "InvalidSequence",
}

// define all properties directly on this class since instances will be stored in the db
export class ApiError extends Error {
  readonly message: string;
  readonly cause: ApiErrorCause;
  readonly id: OeisId;

  constructor(message: string, cause: ApiErrorCause, id: OeisId) {
    super();

    this.message = message;
    this.cause = cause;
    this.id = id;
  }
}

export interface ApiRunner {
  getExpansionById(id: OeisId): Promise<Either<ApiError, OeisFractionalExpansion>>;
  getRandomExpansion(): Promise<OeisFractionalExpansion>;
  warmUp(): void;
}

export class DefaultApiRunner implements ApiRunner {
  #baseUrl: URL;
  #db: ExpansionsDb;
  static #lastFullWarm: Optional<Date> = null;

  constructor(baseURL: URL, db: ExpansionsDb) {
    this.#baseUrl = baseURL;
    this.#db = db;
  }

  warmUp(): void {
    const now = new Date();
    if (!DefaultApiRunner.#lastFullWarm || timeDiff(now, DefaultApiRunner.#lastFullWarm, TimeUnit.Hour) > 1) {
      DefaultApiRunner.#lastFullWarm = now;

      // run a query which should wake upstream services
      fetch(new URL("dozenalExpansions/random", this.#baseUrl));

      return;
    }

    // simply ensure the API is responding
    fetch(new URL("gitInfo", this.#baseUrl));
  }

  async getExpansionById(id: OeisId): Promise<Either<ApiError, OeisFractionalExpansion>> {
    if (!(id instanceof OeisId)) {
      throw new TypeError("id");
    }

    // attempt to see if the expansion is already in the db
    const expansionOrError = await this.#db.getFromDb(id);
    if (expansionOrError) {
      return expansionOrError;
    }

    // call the API to pull down the expansion
    const response = await fetch(new URL(`dozenalExpansions/byId/${id}`, this.#baseUrl));

    const error = await this.#checkApiResponse(response);
    if (error) {
      return { left: error };
    }

    return { right: await this.#getRawExpansionData(response) };
  }

  async getRandomExpansion(): Promise<OeisFractionalExpansion> {
    const response = await fetch(new URL("dozenalExpansions/random", this.#baseUrl));

    const id = OeisId.parse((await response.json()).id);

    // attempt to see if the expansion is already in the db
    const expansion = await this.#db.getFromDb(id);
    if (expansion) {
      return expansion.right!;
    }

    return this.#getRawExpansionData(response);
  }

  async #checkApiResponse(response: Response): Promise<Optional<ApiError>> {
    if (!response.ok) {
      const result = (await response.json()) as { message: string, details: { cause: string, id: string } };

      const error = new ApiError(
        result.message,
        result.details.cause as ApiErrorCause,
        OeisId.parse(result.details.id)
      );

      await this.#db.addToDb(error);

      return error;
    }

    return null;
  }

  async #getRawExpansionData(response: Response): Promise<OeisFractionalExpansion> {
    const blobUrl = new URL(response.headers.get("Content-Location")!);
    response = await fetch(blobUrl);
    const parsed = OeisFractionalExpansion.parseRawText(await response.text());

    await this.#db.addToDb(parsed);

    return parsed;
  }
}
