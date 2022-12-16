<script setup lang="ts">
import ConstIcons from "./icons/ConstIcons.vue";
import { initialOeisId, interestingConstantsInfo, useState } from "@/shared";
import { reactive, ref, watch } from "vue";
import { OeisId } from "@/oeis";

const state = useState();

const inputs = reactive({
  selected: ref(String(initialOeisId)),
  entered: ref(String(initialOeisId)),
});

let timeoutId = 0;
let oldParsedOeisId = initialOeisId;
watch(inputs, () => {
  const parsedOeisId = customRadio.value?.checked
    ? OeisId.parse(inputs.entered)
    : OeisId.parse(inputs.selected);

  // The watch might have been triggered because:
  //   • inputs.entered was set to be the same as inputs.selected when inputs.selected changed
  //   • inputs.entered was changed to pad with zeroes as the user typed
  // But if the actual value of the OeisID has not changed, bail
  if (parsedOeisId.equals(oldParsedOeisId)) {
    return;
  }

  window.clearTimeout(timeoutId);

  oldParsedOeisId = parsedOeisId;

  // TODO is there a better way?
  if (document.querySelector(":invalid")) {
    return;
  }

  inputs.entered = String(parsedOeisId);

  // use a timeout to prevent updating the state with every keystroke
  timeoutId = window.setTimeout(() => state.getExpansionById(parsedOeisId), 200);
});

watch(state, () => {
  if (!state.expansion || state.expansion.id.equals(oldParsedOeisId)) {
    return;
  }

  oldParsedOeisId = state.expansion.id;
  inputs.entered = String(state.expansion.id);
});

function getRandom() {
  window.clearTimeout(timeoutId);

  checkCustomRadio();

  timeoutId = window.setTimeout(() => state.getRandomExpansion(), 200);
}

const customInput = ref<HTMLInputElement>();
const customRadio = ref<HTMLInputElement>();

let enteringCustom = false;
function checkCustomRadio() {
  if (customRadio.value && !enteringCustom) {
    customRadio.value.checked = true;
  }
}

function focusCustomInput() {
  if (customInput.value && customRadio.value?.checked) {
    enteringCustom = true;

    customInput.value.focus();

    enteringCustom = false;
  }
}
</script>

<template>
  <template v-for="item in interestingConstantsInfo" :key="item.tag">
    <label class="form-check-label" :for="item.tag">
      <ConstIcons :tag="item.tag" />
    </label>
    <input type="radio" :value="String(item.id)" :id="item.tag" v-model="inputs.selected" name="constant" />
  </template>
  <label for="custom">custom</label>
  <input type="radio" id="custom" ref="customRadio" name="constant" @change="focusCustomInput"/>
  <input required v-model="inputs.entered" ref="customInput" pattern="[Aa]?0*[1-9]\d{0,8}" @focus="checkCustomRadio"/>
  <button @click="getRandom">random</button>
</template>
