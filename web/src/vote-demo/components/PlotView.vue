<!-- Copyright © 2023 Samuel Justin Gabay
     Licensed under the GNU Affero Public License, Version 3 -->

<script setup lang="ts">
import { useElection } from "@vote-demo/election";
import type { plotter as Plotter } from "@vote-demo/plotResults";
import { nextTick, ref, watch } from "vue";

const election = useElection();
const plot = ref<HTMLDivElement>();
const plotReady = ref(false);

// eslint-disable-next-line vue/no-setup-props-destructure
const { plotter } = defineProps<{
  id: string;
  title: string;
  plotter: Plotter;
}>();

watch(() => election.ballots,
  () => election.hasBallots && nextTick(async () => {
    await plotter(election, plot.value!);
    plotReady.value = true;
  }),
  { immediate: true });
</script>

<template>
  <h5 class="plot-title">{{ title }}</h5>
  <div v-if="!plotReady" class="spinner-border" role="status"></div>
  <div :id="id" ref="plot" class="plot"></div>
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
