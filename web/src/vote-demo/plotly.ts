// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

import { lazy } from "@shared/utils";

const plotlyImport = lazy(async () => {
  // @ts-ignore:
  // Supress missing module type definitions. No typings exist for this subset of plotly,
  // but I can use the typings for the full library
  const { default: plotly } = await import("https://cdn.jsdelivr.net/npm/plotly.js-cartesian-dist-min@2.21.0/+esm")

  return plotly;
});

export function usePlotly(): Promise<typeof import("plotly.js")> {
  return plotlyImport.value
}
