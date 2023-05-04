<!-- Copyright © 2023 Samuel Justin Gabay
     Licensed under the GNU Affero Public License, Version 3 -->

<script setup lang="ts">
import { useElection } from "@vote-demo/election";
import type { plotter as Plotter } from "@vote-demo/plotResults";
import { nextTick, ref, watch } from "vue";

const election = useElection();
const plotHeader = ref<HTMLDivElement>();
const plot = ref<HTMLDivElement>();
const plotReady = ref(false);

const props = defineProps<{
  plotter: Plotter;
  toggleParam?: boolean;
}>();

watch(() => [election.ballots, props.toggleParam],
  () => election.hasBallots && nextTick(async () => {
    await props.plotter(election, plotHeader.value!, plot.value!, props.toggleParam);
    plotReady.value = true;
  }),
  { immediate: true });
</script>

<template>
  <header class="plot-title" ref="plotHeader"><slot /></header>
  <div v-if="!plotReady" class="spinner-border" role="status"></div>
  <div ref="plot" class="plot"></div>
</template>

<style scoped>
.plot-title {
  position: absolute;
  left: 50%;
  transform: translateX(-50%);
  /* Specifying z-index makes the header float on-top of the plot. Otherwise, it will not be visible. */
  z-index: 1;
}

.plot {
  display: inline-block;
  margin: auto;
}

.spinner-border {
  /* Since the plot titles are manually floated to sit on their plots, push the waiting spinner down. */
  margin-top: 2.5em;
  color: var(--color-highlight);
}
</style>
