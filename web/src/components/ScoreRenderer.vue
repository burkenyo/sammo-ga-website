<script setup lang="ts">
import { Engraver } from "@/engraver";
import { initialOeisId, useState } from "@/shared";
import { onMounted, ref, watch } from "vue";

const engraveArea = ref<HTMLDivElement>();

let engraver: Engraver;

onMounted(() => {
  engraver = new Engraver(engraveArea.value!);

  // trigger initial render
  state.getExpansionById(initialOeisId);
});

const state = useState();

const SCORE_NOTES = ["C/5", "C#/5", "D/5", "D#/5", "E/5", "F/4", "F#/4", "G/4", "G#/4", "A/4", "A#/4", "B/4"] as const;

watch([() => state.permutation, () => state.expansion], () => {
  const noteSequence = state.permutation.sequence.map(e => SCORE_NOTES[e]);

  const notes = [...state.expansion!.expansion.digits].map(d => noteSequence[d]);

  engraver.drawNotes(notes, 0, engraveArea.value?.clientWidth!);
});
</script>

<template>
  <div id="engrave-area" ref="engraveArea" />
</template>
