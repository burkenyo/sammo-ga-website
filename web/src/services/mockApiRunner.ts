import { OeisFractionalExpansion, OeisId } from "@/oeis";
import { ApiError, ApiErrorCause, type ApiRunner } from "./apiRunner";

export class MockApiRunner implements ApiRunner {
  async getExpansionById(id: OeisId): Promise<Either<ApiError, OeisFractionalExpansion>> {
    if (!(id instanceof OeisId)) {
      throw new TypeError('id');
    }

    const response = await fetch(`/${id}.txt`);

    if (response.ok) {
      return { right: OeisFractionalExpansion.parseRawText(await response.text()) };
    }

    const error = new ApiError("Not found!", ApiErrorCause.NotFound, id);

    return { left: error };
  }

  async getRandomExpansion(): Promise<OeisFractionalExpansion> {
    const response = await fetch("expansionsList.json.local");
    const idStrings = (await response.json()) as string[];
    const ids = idStrings.map(s => OeisId.parse(s));

    const id = ids[Math.floor(Math.random() * ids.length)];

    const delay = 2000 + Math.random() * 500;
    await new Promise<void>(resolve => window.setTimeout(resolve, delay));

    return (await this.getExpansionById(id)).right!;
  }
}
