<!-- Copyright © 2023 Samuel Justin Gabay
     Licensed under the GNU Affero Public License, Version 3 -->

<script setup lang="ts">
import { useEngraver } from "@melodies/engraver";
import { useState } from "@melodies/state";
import { onMounted, ref, watch } from "vue";
import WaitSpinner from "@shared/components/WaitSpinner.vue";
import { clearChildren } from "@shared/dom-utils";

const engraveArea = ref<HTMLDivElement>();
const engraverReady = ref(false);

const state = useState();

const SCORE_NOTES = ["C/5", "C#/5", "D/5", "D#/5", "E/5", "F/4", "F#/4", "G/4", "G#/4", "A/4", "A#/4", "B/4"] as const;

onMounted(() => watch([() => state.permutation, () => state.expansion], engrave, { immediate: true }));

async function engrave() {
  if (!state.expansion) {
    return;
  }

  engraverReady.value = false;
  clearChildren(engraveArea.value!);
  const engraver = await useEngraver(engraveArea.value!);

  const noteSequence = state.permutation.sequence.map(e => SCORE_NOTES[e]);
  const notes = [...state.expansion.expansion.digits].map(d => noteSequence[d]);

  engraver.drawNotes(notes.slice(0, 500), 0, engraveArea.value!.clientWidth);
  engraverReady.value = true;
}
</script>

<template>
  <WaitSpinner v-if="!engraverReady" />
  <div id="engrave-area" ref="engraveArea" />
</template>
