// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

import { dynamicImport, immutable, ordinalize, range } from "@shared/utils";
import type { ElectionData } from "./election";

const plotlyImport = dynamicImport<{ default: typeof import("plotly.js") }>(
  "https://cdn.jsdelivr.net/npm/plotly.js-cartesian-dist-min@2.21.0/+esm");

namespace shared {
  let plotTitleHeight: number;
  let textWidths: ReadonlyMap<string, number>;

  export function init(election: ElectionData): { plotTitleHeight: number, textWidths: ReadonlyMap<string, number>; } {
    if (plotTitleHeight) {
      return { plotTitleHeight, textWidths };
    }

    const canvas = document.createElement("canvas");
    const context = canvas.getContext("2d")!;
    // default font for Plotly
    context.font = "12px \"Open Sans\", verdana, arial, sans-serif";
    textWidths = new Map(election.nominations.map(n => [n, context.measureText(n).width]));
    canvas.remove();

    const plotTitleStyles = window.getComputedStyle(document.querySelector(".plot-title")!);
    plotTitleHeight = Number.parseFloat(plotTitleStyles.marginTop)
      + Number.parseFloat(plotTitleStyles.lineHeight)
      + Number.parseFloat(plotTitleStyles.marginBottom);

    return { plotTitleHeight, textWidths };
  }
}

export interface plotter {
  (election: ElectionData, element: HTMLDivElement): Promise<void>;
}

export async function plotFirstRoundTallies(election: ElectionData, element: HTMLDivElement): Promise<void> {
  const { nominations, ballots } = election;
  const { plotTitleHeight, textWidths } = shared.init(election);

  const { default: Plotly } = await plotlyImport.value;

  const reversedNominations = [...nominations].reverse();
  const tallies = new Map(reversedNominations.map(n => [n, Array<number>(nominations.length).fill(0)]));

  for (const ballot of ballots)
  for (const [index, nomination] of ballot.entries()) {
    tallies.get(nomination)![index]++;
  }

  // This plot will have all the nominations
  const labelWidth = Math.max(...textWidths.values());
  const gridSize = 32 * Math.min(Math.max(nominations.length, 8), 15)

  Plotly.react(element, [{
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

export async function plotInstantRunoffRounds(election: ElectionData, element: HTMLDivElement): Promise<void> {
  const { nominations, ballots } = election;
  const { plotTitleHeight, textWidths } = shared.init(election);

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

  const { default: Plotly } = await plotlyImport.value;

  const traces = [...irvRunningCounts].map(([nomination, counts]) => ({
    x: range(counts).map(i => i + 1),
    y: counts.map(({ count, total }) =>  count / total),
    name: nomination,
    type: "bar"
  } as const));

  const keys = [...irvRunningCounts.keys()];
  const labelWidth = Math.max(...[...textWidths].filter(([n]) => keys.includes(n)).map(([,w]) => w));
  const gridWidth = 15 * Math.min(Math.max((1 + keys.length) * Math.max(...traces.map(t => t.y.length)), 5), 40)

  const dividers = range(irvTotalRounds - 1).map(i => ({
    type: "line",
    xref: "x", yref: "paper",
    x0: i + 1.5, x1: i + 1.5,
    y0: 0, y1: 1,
    line: { width: 1, color: "lightgrey" }
  } as const));

  Plotly.react(element, traces, {
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

export async function plotBordaCountScores(election: ElectionData, element: HTMLDivElement): Promise<void> {
  const { nominations, ballots } = election;
  const { plotTitleHeight, textWidths } = shared.init(election);

  const { default: Plotly } = await plotlyImport.value;

  let scores = new Map<string, number>();

  for (const ballot of ballots)
  for (const [index, nomination] of ballot.entries())
  {
    const rank = index + 1;
    // scores.set(nomination, (scores.get(nomination) ?? 0) + (nominations.length - rank));
    scores.set(nomination, (scores.get(nomination) ?? 0) + 1 / rank);
  }

  // const max = Math.max(...scores.values())
  // scores = new Map([...scores].sort(([n1], [n2]) => n1.localeCompare(n2)).map(([n, s]) => [n, s / max]));
  scores = new Map([...scores].sort(([n1], [n2]) => n1.localeCompare(n2)));

  const traces = [...scores].map(([nomination, score]) => ({
    y: [score] as number[],
    name: nomination,
    type: "bar",
    hovertemplate: "%{y}"
    // hovertemplate: "%{y:.0%}"
  } as const));

  const keys = [...scores.keys()];
  const labelWidth = Math.max(...[...textWidths].filter(([n]) => keys.includes(n)).map(([,w]) => w));
  const gridWidth = 20 * Math.min(Math.max(keys.length, 8), 20)

  Plotly.react(element, traces, {
    height: 302 + plotTitleHeight,
    width: 200 + gridWidth + labelWidth,
    margin: { t: plotTitleHeight + 8, b: 8 },
    xaxis: { visible: false, fixedrange: true },
    // yaxis: { title: "Normalized Score", range: [0, 1], dtick: 0.1, tickformat: ".0%", fixedrange: true }
    yaxis: { title: "Raw Score", fixedrange: true },
  });
}
