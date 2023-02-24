<!-- Copyright © 2023 Samuel Justin Gabay
     Licensed under the GNU Affero Public License, Version 3 -->

<script setup lang="ts">
defineProps<{
  tag: string;
}>();
</script>

<template>
  <img :src="`/icons/${tag}.svg`" />
</template>

<style scoped>
img {
  height: 1.3em;
  position: relative;
  top: 0.24em;
}
</style>
