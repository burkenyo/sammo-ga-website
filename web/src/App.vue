<script setup lang="ts">
import { computed, defineAsyncComponent, reactive, watch } from "vue";
import { Permutation } from "@/permutation";
import ConstantsListing from "@/components/ConstantsListing.vue";
import { BASE, INITIAL_OEIS_ID, MAX_PERMUTATION, useState } from "@/shared";
import { Fractional } from "./oeis";
import ConstantIcon from "@/components/ConstantIcon.vue";
import OeisLinks from "@/components/OeisLinks.vue";
import Info from "@/markdown/info.md";

const ScoreRenderer = defineAsyncComponent(() => import("./components/ScoreRenderer.vue"));

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

function fixUpName(name: string): string {
  if (name.startsWith("Decimal")) {
    name = "the " + name;
  }

  // remove the word decimal (because we’re in dozenal) and any final period from the description
  return name.replace(/[Dd]ecimal |\.$/g, "");
}

state.getExpansionById(INITIAL_OEIS_ID);
</script>

<template>
  <h3>Info</h3>
  <Info />
  <h3>Sequence</h3>
  <ConstantsListing />
  <h4>Digits</h4>
  <p>
    <template v-if="state.selectedInterestingConstant">
      <ConstantIcon :tag="state.selectedInterestingConstant.tag" />,
      {{ state.selectedInterestingConstant.description }} (<OeisLinks :text="state.selectedInterestingConstant.id" />),
    </template>
    <template v-else-if="state.expansion">
      <OeisLinks :text="state.expansion.id" />, <OeisLinks :text="fixUpName(state.expansion.name)" />
    </template>
    is
    <span id="digits-bold">{{ expansionPreview.text }}</span>
    <template v-if="expansionPreview.abbreviated">...</template>
  </p>
  <h3>Permutation</h3>
  <span class="control-group">
    <label for="permutation-number">number</label>
    <input id="permutation-number" class="wide" required type="number" min="1" :max="MAX_PERMUTATION" v-model="inputs.number" />
  </span>
  <span class="control-group">
    <label for="offset">transposition</label>
    <input id="offset"  class="narrow" required type="number" min="0" :max="BASE - 1" v-model="inputs.offset" />
  </span>
  <span class="control-group">
    <button @click="state.randomizePermutation">random</button>
  </span>
  <span class="control-group">
    <button @click="state.reversePermutation">reverse</button>
    <button @click="state.reflectPermutation">reflect</button>
    <button @click="state.invertPermutation">invert</button>
  </span>
  <h4>Mapping</h4>
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
  <h3>Generated Melody</h3>
  <Suspense>
    <ScoreRenderer />
    <template #fallback>
      <div class="control-group">
        <PacmanLoader color="#0066FF" />
      </div>
    </template>
  </Suspense>
</template>
