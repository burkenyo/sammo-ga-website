// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

import { dynamicImport, immutable, ordinalize, range } from "@shared/utils";
import type { ElectionData } from "./election";

const plotlyImport = dynamicImport<{ default: typeof import("plotly.js") }>(
  "https://cdn.jsdelivr.net/npm/plotly.js-cartesian-dist-min@2.21.0/+esm");

namespace shared {
  const context = document.createElement("canvas").getContext("2d")!;
  // default font for Plotly
  context.font = "12px \"Open Sans\", verdana, arial, sans-serif";

  export function calculateOffsets(
    plotHeaderElement: HTMLElement, relevantNominations: readonly string[]
  ): { plotTitleHeight: number; labelWidth: number } {
    const plotTitleHeight = plotHeaderElement.clientHeight;
    const labelWidth = Math.max(...relevantNominations.map(n => context.measureText(n).width));

    return { plotTitleHeight, labelWidth };
  }
}

export interface plotter {
  (election: ElectionData, plotHeaderElement: HTMLElement, plotElement: HTMLDivElement, toggleParam?: boolean):
    Promise<void>;
}

export async function plotFirstRoundTallies(
  election: ElectionData, plotHeaderElement: HTMLElement, plotElement: HTMLDivElement
): Promise<void> {
  const { nominations, ballots } = election;
  const { plotTitleHeight, labelWidth } = shared.calculateOffsets(plotHeaderElement, nominations);

  const { default: Plotly } = await plotlyImport.value;

  const reversedNominations = [...nominations].reverse();
  const tallies = new Map(reversedNominations.map(n => [n, Array<number>(nominations.length).fill(0)]));

  for (const ballot of ballots)
  for (const [index, nomination] of ballot.entries()) {
    tallies.get(nomination)![index]++;
  }

  // This plot will have all the nominations
  const gridSize = 32 * Math.min(Math.max(nominations.length, 8), 15);

  Plotly.react(plotElement, [{
    x: range(nominations).map(i => ordinalize(i + 1)),
    y: reversedNominations,
    z: [...tallies.values()],
    type: "heatmap",
    colorscale: "Viridis",
    hovertemplate: "nomination: %{y}<br>preference: %{x}<br>votes: %{z}<extra></extra>"
  }], {
    height: 48 + gridSize + plotTitleHeight,
    width: 124 + gridSize + labelWidth,
    margin: { t: plotTitleHeight + 8, b: 40, },
    xaxis: { title: "Preference", fixedrange: true },
    yaxis: { title: "Nomination", automargin: true, fixedrange: true}
  });
}

export async function plotInstantRunoffRounds(
  election: ElectionData, plotHeaderElement: HTMLElement, plotElement: HTMLDivElement
): Promise<void> {
  const { nominations, ballots } = election;

  const [irvRunningCounts, irvTotalRounds, irvWinningRound] = (() => {
    let winningRound: number | undefined = undefined;
    let wipData = ballots;
    let runningCounts: Map<string, { readonly count: number, readonly total: number }[]>;
    let round = 0;
    while (wipData.length) {
      round++;
      const counts = new Map<string, number>();

      for (const [firstChoice] of wipData) {
        counts.set(firstChoice, (counts.get(firstChoice) ?? 0) + 1);
      }

      if (round == 1) {
        runningCounts = new Map([...counts.keys()].sort().map(n => [n, []]));
      }

      for (const [nomination, count] of counts) {
        runningCounts!.get(nomination)!.push({ count, total: wipData.length });
      }

      if (counts.size == 1) {
        winningRound ??= round;
        break;
      } else if (!winningRound && [...counts.values()].some(c => c / wipData.length > 0.5)) {
        winningRound = round;
      }

      const min = Math.min(...counts.values());

      const elim = [...counts].filter(([, c]) => c == min).map(([n]) => n);
      if (round == 1) {
        const keys = [...counts.keys()];

        elim.push(...nominations.filter(n => !keys.includes(n)));
      }

      wipData = wipData
        .map(b => b.filter(n => !elim.includes(n)))
        .filter(b => b.length);
    }

    return [immutable(runningCounts!), round, winningRound];
  })();

  const keys = [...irvRunningCounts.keys()]
  const { plotTitleHeight, labelWidth } = shared.calculateOffsets(plotHeaderElement, keys);

  const { default: Plotly } = await plotlyImport.value;

  const traces = [...irvRunningCounts].map(([nomination, counts]) => ({
    x: range(counts).map(i => i + 1),
    y: counts.map(({ count, total }) =>  count / total),
    name: nomination,
    type: "bar"
  } as const));

  const gridWidth = 15 * Math.min(Math.max((1 + keys.length) * Math.max(...traces.map(t => t.y.length)), 5), 40)

  const dividers = range(irvTotalRounds - 1).map(i => ({
    type: "line",
    xref: "x", yref: "paper",
    x0: i + 1.5, x1: i + 1.5,
    y0: 0, y1: 1,
    line: { width: 1, color: "lightgrey" }
  } as const));

  Plotly.react(plotElement, traces, {
    height: 334 + plotTitleHeight,
    width: 160 + gridWidth + labelWidth,
    margin: { t: plotTitleHeight + 8, b: 40 },
    xaxis: { title: "Round", dtick: 1, fixedrange: true },
    yaxis: { title: "Percentage of Ballots", range: [0, 1], dtick: 0.1, tickformat: ".0%", fixedrange: true },
    shapes: [...dividers, {
      type: "line",
      xref: "paper", yref: "y",
      x0: 0, x1: 1,
      y0: 0.5, y1: 0.5,
      line: { width: 1, color: "grey" }
    }]
  });
}

export async function plotBordaCountScores(
  election: ElectionData, plotHeaderElement: HTMLElement, plotElement: HTMLDivElement, useDowdallCounting?: boolean
): Promise<void> {
  const { nominations, ballots } = election;

  const scores = new Map<string, number>();

  for (const ballot of ballots)
  for (const [index, nomination] of ballot.entries())
  {
    const rank = index + 1;

    if (useDowdallCounting) {
      scores.set(nomination, (scores.get(nomination) ?? 0) + 1 / rank);
    } else {
      scores.set(nomination, (scores.get(nomination) ?? 0) + (nominations.length - rank));
    }
  }

  const keys = [...scores.keys()];
  const { plotTitleHeight, labelWidth } = shared.calculateOffsets(plotHeaderElement, keys);

  const { default: Plotly } = await plotlyImport.value;

  // const max = Math.max(...scores.values())
  const traces = [...scores]
    .sort(([n1], [n2]) => nominations.indexOf(n1) - nominations.indexOf(n2))
    .map(([nomination, score]) => ({
      y: [score] as number[],
      name: nomination,
      type: "bar",
      hovertemplate: "%{y}"
      // hovertemplate: "%{y:.0%}"
    } as const));

  const gridWidth = 20 * Math.min(Math.max(keys.length, 8), 20);

  Plotly.react(plotElement, traces, {
    height: 302 + plotTitleHeight,
    width: 200 + gridWidth + labelWidth,
    margin: { t: plotTitleHeight + 8, b: 8 },
    xaxis: { visible: false, fixedrange: true },
    // yaxis: { title: "Normalized Score", range: [0, 1], dtick: 0.1, tickformat: ".0%", fixedrange: true }
    yaxis: { title: "Raw Score", fixedrange: true },
  });
}
