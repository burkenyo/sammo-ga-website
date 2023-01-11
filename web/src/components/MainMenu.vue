<script setup lang="ts">
import { useRouter } from 'vue-router';
import { useNameUpdater } from '@/nameUpdater';
import { readonly, onBeforeUnmount, ref } from 'vue';
import SocialLink from './SocialLink.vue';

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

const myName = ref("Sammo Gabay");

if (!import.meta.env.SSR) {
  const canceler = useNameUpdater(myName);

  onBeforeUnmount(canceler);
}
</script>

<template>
  <h1>{{ myName }}</h1>
  <nav class="menu">
    <span> <!-- left-side menu items -->
      <span class="menu-item" v-for="route in menuRoutes" :key="route.menuOrder">
        <template v-if="route.menuOrder == $route.meta.menuOrder">{{ route.title }}</template>
        <RouterLink v-else :to="route.path">{{ route.title }}</RouterLink>
      </span>
    </span>
    <span class="align-right"> <!-- right-side menu items -->
      <SocialLink title="GitHub" href="https://github.com/burkenyo" icon-src="/github-mark.svg"/>
      <SocialLink title="LinkedIn" href="https://www.linkedin.com/in/sammo-gabay" icon-src="/linkedin-mark.svg"/>
    </span>
  </nav>
</template>

<style scoped>
nav {
  display: flex;
  justify-content: space-between;
}

span.menu-item + span.menu-item::before {
  content: " | ";
}
</style>
