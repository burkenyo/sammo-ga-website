<!-- Copyright © 2023 Samuel Justin Gabay
     Licensed under the GNU Affero Public License, Version 3 -->

<script setup lang="ts">
import { useElection } from "@vote-demo/election";
import { shuffle } from "@shared/utils";
import { CsvWriter } from "@vote-demo/csvWriter";
import { downloadFile } from "@shared/dom-utils";
import { plotFirstRoundTallies, plotInstantRunoffRounds, plotBordaCountScores } from "@vote-demo/plotResults";
import PlotView from "@vote-demo/components/PlotView.vue";
import { useLocalStorage } from "@vueuse/core";

const useDowdallCount = useLocalStorage("ResultsPage_useDowdallCount", false);

const election = useElection();

async function downloadResults(): Promise<void> {
  const shuffledBallots = [...election.ballots];
  shuffle(shuffledBallots);
  const data = shuffledBallots.map(b => Object.fromEntries(b.map((n, i) => [n, i + 1])));

  const csvWriter = new CsvWriter(election.nominations);
  for (const row of data) {
    csvWriter.writeRow(row);
  }

  downloadFile(csvWriter.getBlob(), "results.csv");
}
</script>

<template>
  <h4>Results</h4>
  <button class="btn btn-primary mx-2" :disabled="!election.hasBallots" @click="downloadResults">Download</button>
  <button class="btn btn-outline-primary mx-2" @click="election.clear()" :disabled="!election.hasBallots">Clear</button>
  <button class="btn btn-outline-primary mx-2" @click="election.reset()" :disabled="election.isExampleData">Reset</button>
  <hr />
  <template v-if="election.hasBallots">
    <PlotView :plotter="plotFirstRoundTallies">
      <h5>First Round Tallies</h5>
    </PlotView>
    <hr />
    <PlotView :plotter="plotInstantRunoffRounds">
      <h5>Instant Runoff Rounds</h5>
    </PlotView>
    <hr />
    <PlotView :plotter="plotBordaCountScores" :toggle-param="useDowdallCount">
      <h5>Borda Count Scores</h5>
      <label for="standardCount" class="me-2">Standard Count</label>
      <input type="radio" class="form-check-input me-4" name="bordaCountType" id="standardCount" :checked="!useDowdallCount" @change="useDowdallCount = false"/>
      <label for="dowdallCount" class="me-2">Dowdall Count</label>
      <input type="radio" class="form-check-input" name="bordaCountType" id="dowdallCount" :checked="useDowdallCount" @change="useDowdallCount = true"/>
    </PlotView>
  </template>
  <template v-else>
    <h5>No Results Yet!</h5>
    <p>No ballots have been cast. Cast a ballot to see results, or click Reset to see the example data.</p>
  </template>
</template>
