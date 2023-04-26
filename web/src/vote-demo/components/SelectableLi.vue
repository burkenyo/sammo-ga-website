<!-- Copyright © 2023 Samuel Justin Gabay
     Licensed under the GNU Affero Public License, Version 3 -->

<script setup lang="ts">
defineProps<{ value: string; selected: boolean }>();

defineEmits<{ (e: "select"): void }>();
</script>

<template>
  <li class="px-2" tabindex="0" :data-selected="selected ? '' : undefined"
      @click="$emit('select')"
      @keydown="($event.code == 'Enter' || $event.code == 'Space') && $emit('select')">
    {{ value }}
  </li>
</template>

<style scoped>
li:hover:not([data-selected]) {
  background-color: var(--bs-highlight-bg);
}

li[data-selected] {
  color: white;
  /* Selected highlight color comes from the arrow button images */
  background-color: #008de3;
}

li[data-selected]::marker {
  color: var(--bs-body-color);
}
</style>
