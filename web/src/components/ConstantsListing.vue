<!-- Copyright © 2023 Samuel Justin Gabay
     Licensed under the GNU Affero Public License, Version 3 -->

<script setup lang="ts">
import { INITIAL_OEIS_ID, interestingConstantsInfo, useState } from "@/shared";
import { reactive, ref, watch } from "vue";
import { OeisId } from "@/oeis";
import PacmanLoader from "vue-spinner/src/PacmanLoader.vue";
import ConstantIcon from "./ConstantIcon.vue";

const state = useState();

const inputs = reactive({
  selected: ref(String(INITIAL_OEIS_ID)),
  entered: ref(String(INITIAL_OEIS_ID)),
});

const loading = ref(false);

let timeoutId = 0;
watch(inputs, () => {
  console.debug("ConstantsListing.vue inputs watch triggered");
  const parsedOeisId = customRadio.value?.checked
    ? OeisId.parse(inputs.entered)
    : OeisId.parse(inputs.selected);

  // The watch might have been triggered because:
  //   • inputs.entered was set to be the same as inputs.selected when inputs.selected changed
  //   • inputs.entered was changed to pad with zeroes as the user typed
  // But if the actual value of the OeisID has not changed, bail
  if (parsedOeisId.equals(state.expansion?.id)) {
    console.debug("ConstantsListing.vue inputs update canceled because of OeisId equality");
    return;
  }

  window.clearTimeout(timeoutId);

  // TODO is there a better way?
  if (document.querySelector(":invalid")) {
    return;
  }

  inputs.entered = String(parsedOeisId);

  // use a timeout to avoid updating the state with every keystroke
  timeoutId = window.setTimeout(async () => {
    try {
      fixupHider();
      loading.value = true;
      state.getExpansionById(parsedOeisId);
    } finally {
      loading.value = false;
    }
  }, 200);
});

watch(() => state.expansion, () => {
  console.debug("[() => state.expansion] watch triggered in ConstantsListing.vue");
  inputs.entered = String(state.expansion!.id);
});

function getRandom() {
  window.clearTimeout(timeoutId);

  checkCustomRadio();

  timeoutId = window.setTimeout(async () => {
    try {
      fixupHider();
      loading.value = true;
      await state.getRandomExpansion();
    } finally {
      loading.value = false;
    }
  }, 50);
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

const controls = ref<HTMLDivElement>();
const hider = ref<HTMLDivElement>();

// HACK: Should use proper CSS!
function fixupHider() {
  hider.value!.style.width = controls.value!.clientWidth + "px";
  hider.value!.style.height = controls.value!.clientHeight + "px";
}
</script>

<template>
  <div style="position: relative">
    <div ref="hider" :hidden="!loading" style="position: absolute; background-color: rgba(255, 255, 255, 0.7)">
      <div style="margin: auto; display: flex; align-items: center">
        <PacmanLoader :loading="loading" color="#0066FF" style="margin: auto" />
      </div>
    </div>
    <div ref="controls" style="width: fit-content">
      <span class="control-group">
        <template v-for="item in interestingConstantsInfo" :key="item.tag">
          <label class="form-check-label" :for="item.tag">
            <ConstantIcon :tag="item.tag"/>
          </label>
          <input type="radio" :value="String(item.id)" :id="item.tag" v-model="inputs.selected" name="constant" :disabled="loading" />
        </template>
      </span>
      <span class="control-group">
        <label for="custom-radio">custom</label>
        <input type="radio" id="custom-radio" ref="customRadio" name="constant" @change="focusCustomInput" :disabled="loading"/>
        <input class="wide" required v-model="inputs.entered" ref="customInput" pattern="[Aa]?0*[1-9]\d{0,8}" @focus="checkCustomRadio" :disabled="loading"/>
        <button @click="getRandom" :disabled="loading">random</button>
      </span>
    </div>
  </div>
</template>
