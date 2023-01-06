<script setup lang="ts">
import { useRouter } from 'vue-router';

const menuRoutes = useRouter()
  .getRoutes()
  .filter(r => Number.isInteger(r.meta.menuOrder))
  .sort((a, b) => (a.meta.menuOrder as number) - (b.meta.menuOrder as number));
</script>

<template>
  <h1>Sammo Gabay</h1>
  <nav>
    <span class="menu" v-for="route in menuRoutes" :key="route.meta.menuOrder as number">
      <template v-if="route.meta.menuOrder == $route.meta.menuOrder">{{ $route.meta.title }}</template>
      <RouterLink v-else :to="route.path">{{ route.meta.title }}</RouterLink>
    </span>
  </nav>
</template>

<style scoped>
span.menu + span.menu::before {
  content: " | ";
}
</style>
