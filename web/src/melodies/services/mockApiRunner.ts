// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

import { OeisFractionalExpansion, OeisId } from "@melodies/oeis";
import { assert, delay } from "@shared/utils";
import { ApiError, ApiErrorCause, type ApiRunner } from "./apiRunner";

export class MockApiRunner implements ApiRunner {
  startWarmUp() { }

  async getExpansionById(id: OeisId): Promise<Either<ApiError, OeisFractionalExpansion>> {
    assert(id instanceof OeisId, "Unexpected type: ", typeof id);

    const response = await fetch(`/expansions/${id}.txt`);

    if (response.ok) {
      return { right: OeisFractionalExpansion.parseRawText(await response.text()) };
    }

    const error = new ApiError("Not found!", ApiErrorCause.NotFound, id);

    return { left: error };
  }

  async getRandomExpansion(): Promise<OeisFractionalExpansion> {
    const response = await fetch("/expansionsList.json.local");
    const idStrings = (await response.json()) as string[];
    const ids = idStrings.map(s => OeisId.parse(s));

    const id = ids[Math.floor(Math.random() * ids.length)];

    await delay(2000 + Math.random() * 500);

    return (await this.getExpansionById(id)).right!;
  }
}
