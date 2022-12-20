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
const expansionPreview = computed(() => {
  if (!state.expansion) {
    return { text: "", abbreviated: false };
  }

  const fullString = String(state.expansion.expansion);

  const displayString = String(state.expansion.expansion)
    .slice(0, 90)
    .split("")
    .map(c => {
      const index = Fractional.DOZENAL_DIGIT_MAP.indexOf(c);

      return index > -1
        ? DISPLAY_DIGIT_MAP.charAt(index)
        : c
    })
    .join("");

  return {text: displayString, abbreviated: fullString.length > displayString.length};
});

watch([() => state.permutation, () => state.expansion], () => {
  console.debug("[() => state.permutation, () => state.expansion] watch triggered in App.vue");

  inputs.number = state.permutation.number;
  inputs.offset = state.permutation.offset;
});
</script>

<template>
  <h3>Sequence</h3>
  <ConstantsListing />
  <h3>Permutation</h3>
  <span class="control-group">
    <label for="permutation-number">Number</label>
    <input id="permutation-number" class="wide" required type="number" min="1" :max="Permutation.getMaxNumber(BASE)" v-model="inputs.number" />
  </span>
  <span class="control-group">
    <label for="offset">Transposition</label>
    <input id="offset"  class="narrow" required type="number" min="0" :max="BASE - 1" v-model="inputs.offset" />
  </span>
  <span class="control-group">
    <button @click="state.randomizePermutation">randomize</button>
    <button @click="state.reversePermutation">reverse</button>
    <button @click="state.reflectPermutation">reflect</button>
    <button @click="state.invertPermutation">invert</button>
  </span>
  <h3>Mapping</h3>
  <table>
    <tr>
      <td class="key" v-for="d in DISPLAY_DIGIT_MAP" :key="d">
        {{ d }}
      </td>
    </tr>
    <tr>
      <td class="key" v-for="note in noteSequence" :key="note">
        {{ note }}
      </td>
    </tr>
  </table>
  <h3>Digits</h3>
  <p>
    <span id="digits-bold">{{ expansionPreview.text }}</span>
    <span v-if="expansionPreview.abbreviated">...</span>
  </p>
  <h3>Generated Melody</h3>
  <ScoreRenderer />
</template>
