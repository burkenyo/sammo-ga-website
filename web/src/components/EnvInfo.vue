<script setup lang="ts">
function getMetaContent(name: string): Optional<string> {
  const element = document.querySelector(`meta[name="${name}"]`) as Optional<HTMLMetaElement>;

  return element?.content;
}

const gitBranch = getMetaContent("git-branch");
const gitCommit = getMetaContent("git-commit");

const isLocal = ["[::]", "127.0.0.1", "localhost"].includes(location.hostname);
</script>

<template>
  <div v-if="gitBranch != 'prime' || isLocal">
    <template v-if="isLocal"> LOCAL </template>
    {{ gitBranch }} {{ gitCommit }}
  </div>
</template>

<style scoped>
div {
  font-weight: bold;
  position: fixed;
  top: 0.2em;
  left: 0.2em;
}
</style>
