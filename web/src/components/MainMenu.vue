<!-- Copyright © 2023 Samuel Justin Gabay
     Licensed under the GNU Affero Public License, Version 3 -->

<script setup lang="ts">
import { useRouter } from 'vue-router';
import { useStaticName, useNameUpdater } from '@/nameUpdater';
import { readonly, onBeforeUnmount, type Ref } from 'vue';
import SocialLink from './SocialLink.vue';

const menuRoutes = readonly(
  useRouter()
    .getRoutes()
    .filter(r => Number.isInteger(r.meta.menuOrder))
    .sort((a, b) => a.meta.menuOrder! - b.meta.menuOrder!)
    .map(r => ({
      menuOrder: r.meta.menuOrder!,
      path: r.path,
      // use nbsp so menu links don’t wrap
      title: (r.meta.title as string).replace(/ /g, "\xA0"),
    }))
);

let myName: string | Ref<String>;

if (import.meta.env.SSR) {
  myName = useStaticName();
} else {
  const { nameRef, canceler } = useNameUpdater();
  myName = nameRef;

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
      <SocialLink title="GitHub" href="https://github.com/burkenyo" icon-src="github-mark.svg"/>
      <SocialLink title="LinkedIn" href="https://www.linkedin.com/in/sammo-gabay" icon-src="linkedin-mark.svg"/>
    </span>
  </nav>
</template>

<style scoped>
nav {
  display: flex;
  justify-content: space-between;
  gap: 2em;
}

span.menu-item + span.menu-item::before {
  content: " | ";
}
</style>
