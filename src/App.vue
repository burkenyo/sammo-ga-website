<script setup lang="ts">
import MainMenu from '@/components/MainMenu.vue';
import { computed, watch } from 'vue';
import { useRoute, RouterView } from 'vue-router';

const route = useRoute();

watch(() => route.meta.title, () => {
  document.title = "SJG – " + route.meta.title;
});

const simpleLayout = computed(() => !!route.meta.simpleLayout);
</script>

<template>
  <template v-if="simpleLayout">
    <div class="full">
      <header>
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
        <RouterView />
      </main>
    </div>
  </template>
</template>

<style>
div.body {
  max-width: 50em;
  min-height:100vh;
  margin: auto;
  background-color: white;
  padding: 1em;
  box-shadow: 0 0 8px #bec0cc;
}

div.full {
  width: 100vw;
  background-color: white;
  padding: 1em;
}
</style>
