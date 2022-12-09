import * as serviceNames from "@/serviceNames"
import { OeisFractionalExpansion, OeisId } from "@/oeis";
import type { ApiRunner } from "@/services/apiRunner";
import { dependencies } from "@/dependencyInjection";

const interestingConstantsInfo = [
  {
    tag: "pi",
    id: new OeisId(796),
    description: "Pi, the ratio of a circle’s circumference to its diameter",
  },
  {
    tag: "e",
    id: new OeisId(1113),
    description: "The base of the natural logarithm",
  },
  {
    tag: "gamma",
    id: new OeisId(1620),
    description: "Gamma, the Euler-Mascheroni constant",
  },
  {
    tag: "phi",
    id: new OeisId(1622),
    description: "Phi, the golden ratio",
  },
  {
    tag: "root_two",
    id: new OeisId(2193),
    description: "The square-root of two",
  },
  {
    tag: "twelfth_root_two",
    id: new OeisId(10774),
    description: "The ratio of the pitches of any two neighboring notes on the chromatic scale",
  },
  {
    tag: "tau",
    id: new OeisId(19692),
    description: "Tau, two times pi, the ratio of a circle’s circumference to its radius",
  },
] as const;

export interface InterestingConstant {
  readonly tag: string;
  readonly expansion: OeisFractionalExpansion;
  readonly description: string;
}

export class InterestingConstants {
  static readonly [dependencies] = [serviceNames.apiRunner] as const;

  private readonly _apiRunner: ApiRunner;

  constructor(apiRunner: ApiRunner) {
    this._apiRunner = apiRunner;
  }

  get = () =>
    interestingConstantsInfo.map(async c => {
      const downloaded = (await this._apiRunner.getExpansionById(c.id)) as OeisFractionalExpansion;

      return { tag: c.tag, expansion: downloaded, description: c.description } as InterestingConstant;
    });
}
