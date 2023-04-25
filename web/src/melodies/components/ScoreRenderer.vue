<!-- Copyright © 2023 Samuel Justin Gabay
     Licensed under the GNU Affero Public License, Version 3 -->

<script setup lang="ts">
import { useEngraverFactory, type Engraver } from "@melodies/engraver";
import { useState } from "@melodies/state";
import { onMounted, ref, watch } from "vue";

const engraveArea = ref<HTMLDivElement>();

// use await here so this component will become an async component
// and be suspensible
const createEngraver = await useEngraverFactory();

let engraver: Engraver;

onMounted(async () => {
  engraver = createEngraver(engraveArea.value!.id);

  engrave();
});

const state = useState();

const SCORE_NOTES = ["C/5", "C#/5", "D/5", "D#/5", "E/5", "F/4", "F#/4", "G/4", "G#/4", "A/4", "A#/4", "B/4"] as const;

watch([() => state.permutation, () => state.expansion], engrave);

async function engrave() {
  if (!state.expansion) {
    return;
  }

  const noteSequence = state.permutation.sequence.map(e => SCORE_NOTES[e]);

  const notes = [...state.expansion.expansion.digits].map(d => noteSequence[d]);

  engraver.drawNotes(notes.slice(0, 500), 0, engraveArea.value!.clientWidth);
}
</script>

<template>
  <div id="engrave-area" ref="engraveArea" />
</template>
