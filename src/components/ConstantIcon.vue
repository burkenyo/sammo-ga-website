<script setup lang="ts">
defineProps<{
  tag: string;
}>();
</script>

<template>
  <img :src="'/' + tag + '.svg'" height="20" />
</template>
