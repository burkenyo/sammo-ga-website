<script setup lang="ts">
import MainMenu from "@/components/MainMenu.vue";
import { computed, watch } from "vue";
import { useRoute } from "vue-router";
import NotFound from "@/pages/NotFound.md";

const route = useRoute();

watch(() => route.meta.title, () => {
  document.title = "SJG – " + route.meta.title;
});

const simpleLayoutHeading = computed(() => route.meta.simpleLayoutHeading as Optional<string>);
</script>

<template>
  <template v-if="simpleLayoutHeading">
    <div class="full">
      <header class="simple">
        <h2>{{ simpleLayoutHeading }}</h2>
        <RouterLink to="/">Home</RouterLink>
      </header>
      <main>
        <RouterView />
      </main>
    </div>
  </template>
  <template v-else>
    <div class="body">
      <header>
        <MainMenu />
      </header>
      <main>
        <RouterView v-if="route.matched.length" />
        <NotFound v-else />
      </main>
    </div>
  </template>
</template>

<style scoped>
header.simple {
  margin: 1em 0;
  border-bottom: 2px solid;
  border-image: linear-gradient(90deg, var(--blue) 0%, var(--blue-lighter) 100%) 1;
}

header.simple > h2 {
  margin-right: 1em;
  display: inline;
  border: none;
}

header.simple > a:hover {
  text-decoration: underline solid transparent 1px;
}

div.body {
  max-width: 50em;
  min-height: calc(100vh - 2em);
  margin: auto;
  background-color: white;
  padding: 1em;
  box-shadow: 0 0 8px var(--grey-darker);
}

div.full {
  width: calc(100vw - 2em);
  min-height: calc(100vh - 2em);
  background-color: white;
  padding: 1em;
}
</style>
