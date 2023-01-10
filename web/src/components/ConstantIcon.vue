<script setup lang="ts">
defineProps<{
  tag: string;
}>();
</script>

<template>
  <img :src="'/' + tag + '.svg'" />
</template>

<style scoped>
img {
  height: 1.3em;
  position: relative;
  top: 0.24em;
}
</style>
