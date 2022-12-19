<script setup lang="ts">
import { computed, reactive, watch } from "vue";
import { Permutation } from "@/permutation";
import ConstantsListing from "@/components/ConstantsListing.vue";
import { BASE, useState } from "@/shared";
import ScoreRenderer from "./components/ScoreRenderer.vue";
import { Fractional } from "./oeis";

const state = useState();

const inputs = reactive({
  number: 1,
  offset: 0,
});

watch(inputs, () => {
  console.debug("App.vue inputs watch triggered");
  // TODO is there a better way?
  if (document.querySelector(":invalid")) {
    return;
  }

  state.updatePermutation(Permutation.create(BASE, inputs.number, inputs.offset));
});

const DISPLAY_NOTES = ["C", "C♯", "D", "D♯", "E", "F", "F♯", "G", "G♯", "A", "A♯", "B"] as const;
const DISPLAY_DIGIT_MAP = "0123456789XL";
const noteSequence = computed(() => state.permutation.sequence.map(e => DISPLAY_NOTES[e]));
const expansionPreview = computed(() => state.expansion
  ? String(state.expansion.expansion)
    .slice(0, 40)
    .split("")
    .map(c => {
      const index = Fractional.DOZENAL_DIGIT_MAP.indexOf(c);

      return index > -1
        ? DISPLAY_DIGIT_MAP.charAt(index)
        : c
    })
    .join("")
  : ""
);

watch([() => state.permutation, () => state.expansion], () => {
  console.debug("[() => state.permutation, () => state.expansion] watch triggered in App.vue");

  inputs.number = state.permutation.number;
  inputs.offset = state.permutation.offset;
});
</script>

<template>
  <ConstantsListing />
  <input required type="number" min="1" :max="Permutation.getMaxNumber(BASE)" v-model="inputs.number" />
  <input required type="number" min="0" :max="BASE - 1" v-model="inputs.offset" />
  <button @click="state.randomizePermutation">randomize</button>
  <button @click="state.reversePermutation">reverse</button>
  <button @click="state.reflectPermutation">reflect</button>
  <button @click="state.invertPermutation">invert</button>
  <h3>Permutation number is: {{ state.permutation.number }}</h3>
  <h3>Selected constant is: {{ String(state.expansion?.id) }}</h3>
  <table>
    <tr>
      <td v-for="d in DISPLAY_DIGIT_MAP" :key="d">
        {{ d }}
      </td>
    </tr>
    <tr>
      <td v-for="note in noteSequence" :key="note">
        {{ note }}
      </td>
    </tr>
  </table>
  <p>{{ expansionPreview }}</p>
  <ScoreRenderer />
</template>

<style>
input:invalid {
  border: 2px dashed red;
}
</style>
