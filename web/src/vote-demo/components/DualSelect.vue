<!-- Copyright © 2023 Samuel Justin Gabay
     Licensed under the GNU Affero Public License, Version 3 -->

<script setup lang="ts">
import SelectableLi from "@vote-demo/components/SelectableLi.vue";
import { computed, ref } from "vue";

const props = defineProps<{options: readonly string[], optionsTitle: string, selectionsTitle: string }>();

const selections = ref<string[]>([]);
const options = computed(() => props.options.filter(n => !selections.value.includes(n)));

const selectedOption = ref<string>();
const selectedSelection = ref<string>();

function pick() {
  if (!selectedOption.value) {
    return;
  }

  const index = options.value.indexOf(selectedOption.value);
  selections.value.push(selectedOption.value);
  selectedOption.value = options.value[Math.min(index, options.value.length - 1)];
}

function remove() {
  if (!selectedSelection.value) {
    return;
  }

  const index = selections.value.indexOf(selectedSelection.value);
  selections.value.splice(index, 1);
  selectedSelection.value = selections.value[Math.min(index, selections.value.length - 1)];
}

function up() {
  if (!selectedSelection.value) {
    return;
  }

  const index = selections.value.indexOf(selectedSelection.value);
  if (index == 0) {
    return;
  }

  selections.value.splice(index - 1, 2, selectedSelection.value, selections.value[index - 1]);
}

function down() {
  if (!selectedSelection.value) {
    return;
  }

  const index = selections.value.indexOf(selectedSelection.value);
  if (index == selections.value.length - 1) {
    return;
  }

  selections.value.splice(index, 2, selections.value[index + 1], selectedSelection.value);
}

function clear() {
  selectedOption.value = undefined;
  selectedSelection.value = undefined;
  selections.value = [];
}

defineExpose({ clear, selections });
</script>

<template>
  <fieldset class="selection-area">
    <label style="grid-area: options-label;"><h5>{{ props.optionsTitle }}</h5></label>
    <div class="list-holder" style="grid-area: options">
      <ul class="selection-list list-unstyled">
        <SelectableLi v-for="item of options" :key="item" :value="item" :selected="item == selectedOption"
          @select="selectedOption = item" />
      </ul>
    </div>

    <div class="button" style="grid-area: pick">
      <button type="button" @click="pick"><img src="/right.png" alt="pick" /></button>
    </div>
    <div class="button" style="grid-area: remove">
      <button type="button" @click="remove"><img src="/left.png" alt="remove" /></button>
    </div>

    <label style="grid-area: selections-label"><h5>{{ props.selectionsTitle }}</h5></label>
    <div class="list-holder" style="grid-area: selections">
      <ol class="selection-list" >
        <SelectableLi v-for="item of selections" :key="item" :value="item" :selected="item == selectedSelection"
          @select="selectedSelection = item" />
      </ol>
    </div>

    <div class="button" style="grid-area: up">
      <button type="button" @click="up"><img src="/up.png" alt="up" /></button>
    </div>
    <div class="button" style="grid-area: down">
      <button type="button" @click="down"><img src="/down.png" alt="down" /></button>
    </div>
  </fieldset>
</template>

<style scoped>
.selection-area {
  margin: auto;
  width: fit-content;
  display: grid;
  grid-template-areas:
    "options-label .      selections-label selections-label selections-label selections-label"
    "options       .      selections       selections       selections       selections"
    "options       pick   selections       selections       selections       selections"
    "options       remove selections       selections       selections       selections"
    "options       .      selections       selections       selections       selections"
    ".             .      .                up               down             .";
  grid-template-columns: 12em 48px calc(7em - 48px) 48px 48px calc(7em - 48px);
  grid-template-rows: auto calc(7.5em - 48px) 48px 48px calc(7.5em - 48px) 48px;
}

.selection-area div.button {
  margin: auto;
}

.selection-area button {
  margin: auto;
  appearance: none;
  padding: 4px;
  border: none;
  background-color: transparent;
}

.selection-area button:active img {
  filter: brightness(85%);
}

button img {
  height: 32px;
  width: 32px;
}

.selection-list {
  cursor: default;
  text-align: left;
  white-space: nowrap;
  overflow: hidden;
}

div.list-holder {
  border: 1px solid slategrey;
  padding: 0.2em;
}
</style>
