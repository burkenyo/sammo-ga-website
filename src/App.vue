<script setup lang="ts">
import { reactive, ref, watch } from "vue";
import { Permutation } from "@/permutation";
import ConstantsListing from "@/components/ConstantsListing.vue";
import { BASE, initialOeisId, useState } from "@/shared";
import type { OeisId } from "./oeis";

const state = useState();

const inputs = reactive({
  number: 1,
  offset: 0,
});

watch(inputs, () => {
  // TODO is there a better way?
  if (document.querySelector(":invalid")) {
    return;
  }

  // if inputs watch was triggered because states watch was, the permutation will already be updated
  if (inputs.number != state.permutation.number || inputs.offset != state.permutation.offset) {
    state.permutation = Permutation.create(BASE, inputs.number, inputs.offset);
  }
});

const melody = ref([] as readonly string[]);
const expansionPreview = ref("");

let oldOeisId: Optional<OeisId>;
let oldPermutation: Optional<Permutation>;
watch(state, () => {
  if (!state.expansion || (state.expansion.id.equals(oldOeisId) && state.permutation.equals(oldPermutation))) {
    return;
  }

  oldOeisId = state.expansion.id;
  oldPermutation = state.permutation;
  expansionPreview.value = String(state.expansion.expansion).slice(0, 40);
  melody.value = [...state.expansion.expansion.digits].map(d => state.noteSequence[d]);
});

state.getExpansionById(initialOeisId);
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
      <td v-for="i in BASE" :key="i">
        {{ i - 1 }}
      </td>
    </tr>
    <tr>
      <td v-for="note in state.noteSequence" :key="note">
        {{ note }}
      </td>
    </tr>
  </table>
  <p>{{ expansionPreview }}</p>
  <p>{{ melody }}</p>
</template>

<style>
input:invalid {
  border: 2px dashed red;
}
</style>
