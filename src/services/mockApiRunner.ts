import { OeisFractionalExpansion, OeisId } from "./oeis";
import { ApiError, ApiErrorCause, type ApiRunner } from "./apiRunner";

export class MockApiRunner implements ApiRunner {
  async getExpansionById(id: OeisId): Promise<OeisFractionalExpansion | ApiError> {
    if (!(id instanceof OeisId)) {
      throw new TypeError('id');
    }

    const response = await fetch(`/${id}.txt`);

    if (response.ok) {
      return OeisFractionalExpansion.parseRawText(await response.text());
    }

    const error = new ApiError("Not found!", ApiErrorCause.NotFound, id);

    return error;
  }

  async getRandomExpansion(): Promise<OeisFractionalExpansion> {
    const response = await fetch("expansionsList.json.local");
    const idStrings = (await response.json()) as string[];
    const ids = idStrings.map(s => OeisId.parse(s));

    const id = ids[Math.floor(Math.random() * ids.length)];

    return this.getExpansionById(id) as Promise<OeisFractionalExpansion>;
  }
}
