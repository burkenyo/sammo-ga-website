<!-- Copyright © 2023 Samuel Justin Gabay
     Licensed under the GNU Affero Public License, Version 3 -->

<script setup lang="ts">
import { defineAsyncComponent, ref, onMounted } from "vue";

// only show the icon AFTER it’s size has been calculated
const show = ref(false);

// eslint-disable-next-line vue/no-setup-props-destructure
const { iconSrc } = defineProps<{
  title: string;
  href: string;
  iconSrc: string;
}>();

const Icon = defineAsyncComponent(() => import(`@main/assets/${iconSrc}.vue`));

onMounted(() => {
  show.value = true;
});
</script>

<template>
  <a v-href="href" :title="title">
    <Icon v-show="show" />
  </a>
</template>

<style scoped>
svg {
  height: 1.25em;
  position: relative;
  top: -0.1em;
  fill: var(--blue);
  transition: fill 150ms;
}

svg:hover {
  fill: var(--blue-lighter);
  transition: fill 150ms;
}

a + a {
  margin-left: 0.8em;
}
</style>
