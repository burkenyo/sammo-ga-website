<!-- Copyright © 2023 Samuel Justin Gabay
     Licensed under the GNU Affero Public License, Version 3 -->

<script setup lang="ts">
import DualSelect from "@vote-demo/components/DualSelect.vue";
import { ref } from "vue";
import { useElection } from "@vote-demo/election";

const election = useElection();

const nominations = election.nominations;

const selectionsArea = ref<InstanceType<typeof DualSelect>>();

const noSelections = ref(false);

const showSuccess = ref(false);

function submit(): void {
  noSelections.value = !selectionsArea.value?.selections.length;

  if (noSelections.value) {
    return;
  }

  election.logBallot(crypto.randomUUID(), selectionsArea.value?.selections!, false);

  showSuccess.value = true;
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
    <div class="alert alert-success alert-dismissible" role="alert" v-if="showSuccess">
      <strong>Success!</strong> You’ve cast your vote. Now, try viewing the
      <RouterLink :to="{ name: 'results' }" class="alert-link-subtle">results</RouterLink>, or vote again.
      <button type="button" class="btn-close" @click="selectionsArea!.clear(); showSuccess = false"></button>
    </div>
    <template v-else>
      <button id="clear" type="button" class="btn btn-secondary mx-2" @click="selectionsArea!.clear()">Clear</button>
      <button id="submit" type="submit" class="btn btn-primary mx-2">Cast Ballot</button>
    </template>
  </form>
</template>

<style scoped>
div.instructions {
  margin: auto;
  max-width: 30em;
  text-align: left;
}

div.alert {
  max-width: 40em;
  margin-left: auto;
  margin-right: auto;
}
</style>
