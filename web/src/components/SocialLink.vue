<!-- Copyright © 2023 Samuel Justin Gabay
     Licensed under the GNU Affero Public License, Version 3 -->

<script setup lang="ts">
import { defineAsyncComponent } from "vue";

const props = defineProps<{
  title: string;
  href: string;
  iconSrc: string;
}>();

const Icon = defineAsyncComponent(() => import(`@/assets/${props.iconSrc}.vue`));
</script>

<template>
  <a v-href="href" :title="title">
    <Icon />
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
