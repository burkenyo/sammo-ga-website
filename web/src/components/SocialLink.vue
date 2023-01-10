<script setup lang="ts">
defineProps<{
  title: string;
  href: string;
  iconSrc: string;
}>();
</script>

<template>
  <a :href="href" target="_blank" :title="title">
    <img :src="iconSrc" />
  </a>
</template>

<style scoped>
img {
  height: 1.25em;
  position: relative;
  top: -0.1em;
}

a {
  margin-left: 0.8em;
}
</style>
