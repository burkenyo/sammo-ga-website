<!-- Copyright © 2023 Samuel Justin Gabay
     Licensed under the GNU Affero Public License, Version 3 -->

<script setup lang="ts">
import EnvInfo from "./components/EnvInfo.vue";
import { useGitInfoMeta as useGitInfoMeta } from "./build";
import HeaderWithContent from "./components/HeaderWithContent.vue";
import type { Component } from "vue";

defineProps<{
  heading: string;
  root: Component;
}>();

useGitInfoMeta();
</script>

<template>
  <EnvInfo />
  <div class="layout-simple">
    <header>
      <HeaderWithContent>
        <h2>{{ heading }}</h2>
        <a href="/">Home</a>
      </HeaderWithContent>
    </header>
    <main>
      <component :is="root" />
    </main>
  </div>
</template>
