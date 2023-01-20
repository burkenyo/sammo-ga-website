<script setup lang="ts">
defineProps<{ value: boolean }>();
</script>

<template>
  <span v-if="value" class="checked">✓</span>
  <span v-else class="crossed">✗</span>
</template>

<style scoped>
span.checked {
  font-weight: 900;
  color: green;
}

span.crossed {
  font-weight: 900;
  color: firebrick;
}
</style>
