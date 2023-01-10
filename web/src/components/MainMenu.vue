<script setup lang="ts">
import { useRouter } from 'vue-router';
import { useNameUpdater } from '@/nameUpdater';
import { onBeforeUnmount, ref } from 'vue';
import { readonly } from '@/utils';

const menuRoutes = readonly(
  useRouter()
    .getRoutes()
    .filter(r => Number.isInteger(r.meta.menuOrder))
    .sort((a, b) => (a.meta.menuOrder as number) - (b.meta.menuOrder as number))
    .map(r => ({
      menuOrder: r.meta.menuOrder as number,
      path: r.path,
      // use nbsp so menu links don’t wrap
      title: (r.meta.title as string).replace(/ /g, "\xA0"),
    }))
);

const myName = ref("");

const canceler = useNameUpdater(myName);

onBeforeUnmount(canceler);
</script>

<template>
  <h1>{{ myName }}</h1>
  <nav>
    <span class="menu" v-for="route in menuRoutes" :key="route.menuOrder">
      <template v-if="route.menuOrder == $route.meta.menuOrder">{{ route.title }}</template>
      <RouterLink v-else :to="route.path">{{ route.title }}</RouterLink>
    </span>
  </nav>
</template>

<style scoped>
span.menu + span.menu::before {
  content: " | ";
}
</style>
