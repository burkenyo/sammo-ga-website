<script setup lang="ts">
import MainMenu from "@/components/MainMenu.vue";
import { computed } from "vue";
import { RouterLink, RouterView, useRoute, useRouter } from "vue-router";
import NotFound from "@/pages/NotFound.md";
import { useHead } from "@vueuse/head";
import EnvInfo from "./components/EnvInfo.vue";
import { useGitInfoMeta as useGitInfoMeta } from "./shared";
import HeaderWithContent from "./components/HeaderWithContent.vue";

const route = useRoute();
const routeFound = computed(() => !!route.matched.length);

const simpleLayoutHeading = computed(() => route.meta.simpleLayoutHeading);
const useSimpleLayout = computed(() => !!simpleLayoutHeading.value);
const layoutClass = computed(() =>
  useSimpleLayout.value ? "layout-simple" : "layout-standard"
);

const notFound = useRouter().getRoutes().find(r => r.name ==  NotFound.__name)!.meta;

useHead({
  title: computed(() => (route.meta.title ?? notFound.title) + " – Sammo Gabay"),
  meta: [
    { name: "description", content: computed(() => route.meta.description ?? notFound.description) },
  ],
});

useGitInfoMeta();
</script>

<template>
  <client-only><EnvInfo /></client-only>
  <div :class="layoutClass">
    <HeaderWithContent v-if="useSimpleLayout">
      <template #heading>{{ simpleLayoutHeading }}</template>
      <RouterLink to="/">Home</RouterLink>
    </HeaderWithContent>
    <header v-else>
      <MainMenu />
    </header>
    <main>
      <RouterView v-if="routeFound" />
      <NotFound v-else />
    </main>
  </div>
</template>

<style scoped>
div.layout-standard {
  max-width: 50em;
  min-height: calc(100vh - 2em);
  margin: auto;
  background-color: white;
  padding: 1em;
  box-shadow: 0 0 8px var(--grey-darker);
}

div.layout-simple {
  width: calc(100vw - 2em);
  min-height: calc(100vh - 2em);
  background-color: white;
  padding: 1em;
}
</style>
