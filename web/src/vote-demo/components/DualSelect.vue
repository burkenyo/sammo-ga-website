<!-- Copyright © 2023 Samuel Justin Gabay
     Licensed under the GNU Affero Public License, Version 3 -->

<script setup lang="ts">
import SelectableLi from "@vote-demo/components/SelectableLi.vue";
import { computed, ref, type Ref } from "vue";
import { useLocalStorage } from "@vueuse/core";

const emit = defineEmits<{
  (e: "selectionsupdated", selections: readonly string[]): void;
}>();

const props = defineProps<{
  storageKey?: string;
  options: readonly string[];
  optionsTitle: string;
  selectionsTitle: string;
}>();

let state: Ref<{
  selections: string[];
  selectedOption?: string;
  selectedSelection?: string;
}>;

if (props.storageKey) {
  state = useLocalStorage(props.storageKey, { selections: [] });
} else {
  state = ref({ selections: [] });
}

const options = computed(() => props.options.filter(n => !state.value.selections.includes(n)));

function pick() {
  if (!state.value.selectedOption) {
    return;
  }

  const index = options.value.indexOf(state.value.selectedOption);
  state.value.selections.push(state.value.selectedOption);
  state.value.selectedOption = options.value[Math.min(index, options.value.length - 1)];
  emit("selectionsupdated", state.value.selections);
}

function remove() {
  if (!state.value.selectedSelection) {
    return;
  }

  const index = state.value.selections.indexOf(state.value.selectedSelection);
  state.value.selections.splice(index, 1);
  state.value.selectedSelection = state.value.selections[Math.min(index, state.value.selections.length - 1)];
  emit("selectionsupdated", state.value.selections);
}

function up() {
  if (!state.value.selectedSelection) {
    return;
  }

  const index = state.value.selections.indexOf(state.value.selectedSelection);
  if (index == 0) {
    return;
  }

  state.value.selections.splice(index - 1, 2, state.value.selectedSelection, state.value.selections[index - 1]);
  emit("selectionsupdated", state.value.selections);
}

function down() {
  if (!state.value.selectedSelection) {
    return;
  }

  const index = state.value.selections.indexOf(state.value.selectedSelection);
  if (index == state.value.selections.length - 1) {
    return;
  }

  state.value.selections.splice(index, 2, state.value.selections[index + 1], state.value.selectedSelection);
  emit("selectionsupdated", state.value.selections);
}

defineExpose({
  clear() {
    state.value = { selections: [] };
    emit("selectionsupdated", state.value.selections);
  },
  get selections(): readonly string[] {
    return state.value.selections;
  },
});
</script>

<template>
  <fieldset class="selection-area">
    <label style="grid-area: options-label;"><h5>{{ props.optionsTitle }}</h5></label>
    <div class="list-holder" style="grid-area: options">
      <ul class="selection-list list-unstyled">
        <SelectableLi v-for="item of options" :key="item" :value="item" :selected="item == state.selectedOption"
          @select="state.selectedOption = item" />
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
      <ol class="selection-list">
        <SelectableLi v-for="item of state.selections" :key="item" :value="item" :selected="item == state.selectedSelection"
          @select="state.selectedSelection = item" />
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
