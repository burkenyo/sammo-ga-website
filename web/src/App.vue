<script setup lang="ts">
import HelloWorld from "@/components/HelloWorld.vue";
import ConstantsListing from "@/components/ConstantsListing.vue";
import * as oeis from "@/oeis";
import { useServices } from "@/services";
import { Permutation } from "./permutation";

if (import.meta.env.DEV) {
  const debugHelpers = Object.freeze({
    oeis: oeis,
    Permutation: Permutation,
    getService: (key: any) => useServices().retrieve(key),
  });

  Object.defineProperty(window, "debugHelpers", { value: debugHelpers });
}
</script>

<template>
  <header>
    <img
      alt="Vue logo"
      class="logo"
      src="./assets/logo.svg"
      width="125"
      height="125"
    />

    <div class="wrapper">
      <HelloWorld />
    </div>
  </header>

  <main>
    <Suspense>
      <ConstantsListing />
    </Suspense>
  </main>
</template>

<style scoped>
header {
  line-height: 1.5;
}

.logo {
  display: block;
  margin: 0 auto 2rem;
}

@media (min-width: 1024px) {
  header {
    display: flex;
    place-items: center;
    padding-right: calc(var(--section-gap) / 2);
  }

  .logo {
    margin: 0 2rem 0 0;
  }

  header .wrapper {
    display: flex;
    place-items: flex-start;
    flex-wrap: wrap;
  }
}
</style>
