<!-- Copyright © 2023 Samuel Justin Gabay
     Licensed under the GNU Affero Public License, Version 3 -->

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
