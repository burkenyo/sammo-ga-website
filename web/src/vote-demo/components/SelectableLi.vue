<!-- Copyright © 2023 Samuel Justin Gabay
     Licensed under the GNU Affero Public License, Version 3 -->

<script setup lang="ts">
defineProps<{ value: string; selected: boolean }>();

defineEmits<{ (e: "select"): void }>();
</script>

<template>
  <li tabindex="0" :data-selected="selected ? '' : undefined"
      @click="$emit('select')"
      @keydown="($event.code == 'Enter' || $event.code == 'Space') && $emit('select')">
    {{ value }}
  </li>
</template>

<style scoped>
li:hover:not([data-selected]) {
  background-color: cornsilk;
}

li[data-selected] {
  background-color: lightsteelblue;
}
</style>
