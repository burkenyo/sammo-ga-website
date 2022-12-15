<script setup lang="ts">
import ConstIcons from "./icons/ConstIcons.vue";
import { selected, interestingConstantsInfo } from "@/shared";
</script>

<template>
  <template v-for="item in interestingConstantsInfo" :key="item.tag">
    <label class="form-check-label" :for="item.tag" checked>
      <ConstIcons :tag="item.tag" />
    </label>
    <input type="radio" :value="item.id" :id="item.tag" v-model="selected" name="constant" />
  </template>
</template>
