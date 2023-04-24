<!-- Copyright © 2023 Samuel Justin Gabay
     Licensed under the GNU Affero Public License, Version 3 -->

<script setup lang="ts">
import { useBuildInfo } from "@shared/build";
import CheckIcon from "./CheckIcon.vue";

const { gitBranch, gitCommit, isDirty, isBuilt } = useBuildInfo();

const isLocal = ["[::]", "127.0.0.1", "localhost"].includes(location.hostname);
</script>

<template>
  <div v-if="gitBranch != 'prime' || isLocal">
    {{ gitBranch }} {{ gitCommit }}<br />
    <table>
      <tr><th>clean</th><td><CheckIcon :value="!isDirty" /></td></tr>
      <tr><th>built</th><td><CheckIcon :value="isBuilt" /></td></tr>
      <tr><th>hosted</th><td><CheckIcon :value="!isLocal" /></td></tr>
    </table>
  </div>
</template>

<style scoped>
div {
  font-weight: bold;
  position: fixed;
  top: 0.3em;
  left: 0.3em;
  padding: 0.2em;
  background-color: rgba(255, 255, 255, 0.5);
  box-shadow: 0px 0px 2px 2px rgba(255, 255, 255, 0.5);
}

th {
  padding-right: 0.2em;
  text-align: right;
}

td {
  padding-left: 0.2em;
  text-align: left;
}
</style>
