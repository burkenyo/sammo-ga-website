<!-- Copyright © 2023 Samuel Justin Gabay
     Licensed under the GNU Affero Public License, Version 3 -->

<script setup lang="ts">
import MainMenu from "@main/components/MainMenu.vue";
import { computed } from "vue";
import { RouterView, useRoute, useRouter } from "vue-router";
import NotFound from "@main/pages/NotFound.md";
import EnvInfo from "@shared/components/EnvInfo.vue";
import { useGitInfoMeta as useGitInfoMeta } from "@shared/build";
import { useHead } from "@vueuse/head";

const route = useRoute();
const routeFound = computed(() => !!route.matched.length);

const notFound = useRouter().getRoutes().find(r => r.name ==  NotFound.__name)!.meta;

useHead({
  title: computed(() => (route.meta.title ?? notFound.title) + " – Sammo Gabay"),
  meta: [{ name: "description", content: computed(() => route.meta.description ?? notFound.description) }],
});

useGitInfoMeta();
</script>

<template>
  <client-only><EnvInfo /></client-only>
  <div class="layout-standard">
    <header>
      <MainMenu />
    </header>
    <main>
      <RouterView v-if="routeFound" />
      <NotFound v-else />
    </main>
  </div>
</template>
