import { OeisFractionalExpansion, OeisId } from "./oeis";
import type { ExpansionsDb } from "./expansionsDb";

export enum ApiErrorCause {
  NotFound = 'NotFound',
  InvalidSequence = 'InvalidSequence',
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
  getExpansionById(id: OeisId): Promise<OeisFractionalExpansion | ApiError>;
  getRandomExpansion(): Promise<OeisFractionalExpansion>;
}

export class DefaultApiRunner implements ApiRunner {
  private _baseUrl: URL;
  private _db: ExpansionsDb;

  constructor(baseURL: URL, db: ExpansionsDb) {
    this._baseUrl = baseURL;
    this._db = db;
  }

  async getExpansionById(id: OeisId): Promise<OeisFractionalExpansion | ApiError> {
    if (!(id instanceof OeisId)) {
      throw new TypeError('id');
    }

    // attempt to see if the expansion is already in the db
    const expansionOrError = await this._db.getFromDb(id);
    if (expansionOrError) {
      return expansionOrError;
    }

    // call the API to pull down the expansion
    const response = await fetch(new URL(`dozenalExpansions/byId/${id}`, this._baseUrl));

    const error = await this.checkApiResponse(response);
    if (error) {
      return error;
    }

    return this.getRawExpansionData(response);
  }

  async getRandomExpansion(): Promise<OeisFractionalExpansion> {
    const response = await fetch(new URL('dozenalExpansions/random', this._baseUrl));

    const id = OeisId.parse((await response.json()).id);

    // attempt to see if the expansion is already in the db
    const expansion = await this._db.getFromDb(id);
    if (expansion) {
      return expansion as OeisFractionalExpansion;
    }

    return this.getRawExpansionData(response);
  }

  private async checkApiResponse(response: Response): Promise<ApiError | undefined> {
    if (!response.ok) {
      const result = (await response.json()) as { message: string, details: { cause: string, id: string } };

      const error = new ApiError(
        result.message,
        result.details.cause as ApiErrorCause,
        OeisId.parse(result.details.id)
      );

      await this._db.addToDb(error);

      return error;
    }
  }

  private async getRawExpansionData(response: Response): Promise<OeisFractionalExpansion> {
    const blobUrl = new URL(response.headers.get('Content-Location')!);
    response = await fetch(blobUrl);
    const parsed = OeisFractionalExpansion.parseRawText(await response.text());

    await this._db.addToDb(parsed);

    return parsed;
  }
}
