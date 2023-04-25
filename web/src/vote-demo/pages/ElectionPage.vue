<!-- Copyright © 2023 Samuel Justin Gabay
     Licensed under the GNU Affero Public License, Version 3 -->

<script setup lang="ts">
import DualSelect from "@vote-demo/components/DualSelect.vue";
import { ref } from "vue";
import router from "@vote-demo/router";
import { useState } from "@vote-demo/state";
import { useElection } from "@vote-demo/election";

const state = useState();
const election = useElection();

const nominations = election.nominations;

const selectionsArea = ref<InstanceType<typeof DualSelect>>();

const noSelections = ref(false);

function submit(): void {
  noSelections.value = !selectionsArea.value?.selections.length

  if (noSelections.value) {
    return;
  }

  election.logBallot(crypto.randomUUID(), selectionsArea.value?.selections!, false)

  state.showSuccess = true;
  router.push({ name: "success" });
}
</script>

<template>
  <h4>Choosing a new Mascot</h4>
  <div class="instructions">
    In this example, you are helping choose a new school mascot.
    Rank your preferences for mascot below:
    <ul>
      <li>Move your selections between the nominations list and the
        preferences list using the left and right arrow buttons.</li>
      <li>Pick as many selections as you like.</li>
      <li>Use the up and down arrow buttons to rank your preferences.</li>
    </ul>
  </div>
  <hr />
  <form @submit="$event.preventDefault(); submit()">
    <div style="margin: auto">
      <div v-if="noSelections" class="alert alert-danger" role="alert">
        Make at least one selection!
      </div>
      <DualSelect :options="nominations" ref="selectionsArea"
        options-title="Nominations" selections-title="Your Preferences" />
    </div>
    <hr />

    <button id="clear" type="button" class="btn btn-secondary" @click="selectionsArea?.clear()">Clear</button>
    <button id="submit" type="submit" class="btn btn-primary" style="margin-left: 3em">Submit Vote</button>
  </form>
</template>

<style scoped>
div.instructions {
  margin: auto;
  max-width: 30em;
  text-align: left;
}
</style>
