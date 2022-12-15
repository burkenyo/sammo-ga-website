<script setup lang="ts">
import { ref, computed, watch, reactive } from "vue";
import { Permutation } from "@/permutation";
import ConstantsListing from "@/components/ConstantsListing.vue";
import { selected } from "@/shared";
import { serviceKeys, useServices } from "@/services";
import type { OeisFractionalExpansion } from "./oeis";

const NOTES = ["C", "C♯", "D", "D♯", "E", "F", "F♯", "G", "G♯", "A", "A♯", "B"] as const;
const BASE = NOTES.length;

// a prermutation is purposefully immutable, therefore it should not be made reactive, but wrapped in a ref;
const permutation = ref(Permutation.create(BASE, 1, 0));

const noteSequence = computed(() => permutation.value.sequence.map(e => NOTES[e]));

let cameFromButton = false;

const inputs = reactive({
  number: 1,
  offset: 0,
});

watch(inputs, () => {
  if (cameFromButton) {
    cameFromButton = false;
    console.log("bail!");

    return;
  }

  // TODO is there a better way?
  if (document.querySelector(":invalid")) {
    return;
  }

  permutation.value = Permutation.create(BASE, inputs.number, inputs.offset);
});

function randomize() {
  updatePermutationFromButtons(Permutation.createRandom(BASE));
}

function reverse() {
  updatePermutationFromButtons(permutation.value.reverse());
}

function reflect() {
  updatePermutationFromButtons(permutation.value.reflect());
}

function invert() {
  updatePermutationFromButtons(permutation.value.invert());
}

function updatePermutationFromButtons(newPermutation: Permutation) {
  permutation.value = newPermutation;

  inputs.number = newPermutation.number;
  inputs.offset = newPermutation.offset;

  cameFromButton = true;
}

const apiRunner = useServices().retrieve(serviceKeys.apiRunner);

const melody = ref([] as readonly string[]);
const expansionPreview = ref("");

watch([permutation, selected], async () => {
    // TODO error-handling
    const data = await apiRunner.getExpansionById(selected.value) as OeisFractionalExpansion;

    expansionPreview.value = String(data.expansion).slice(0, 40);
    melody.value = [...data.expansion.digits].map(d => noteSequence.value[d]);
  },
  { immediate: true }
);
</script>

<template>
  <ConstantsListing />
  <input required type="number" min="1" :max="Permutation.getMaxNumber(BASE)" v-model="inputs.number" />
  <input required type="number" min="0" :max="BASE - 1" v-model="inputs.offset" />
  <button @click="randomize">randomize</button>
  <button @click="reverse">reverse</button>
  <button @click="reflect">reflect</button>
  <button @click="invert">invert</button>
  <h3>Permutation number is: {{ permutation.number }}</h3>
  <h3>Selected constant is: {{ selected }}</h3>
  <table>
    <tr>
      <td v-for="i in BASE" :key="i">
        {{ i - 1 }}
      </td>
    </tr>
    <tr>
      <td v-for="note in noteSequence" :key="note">
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
