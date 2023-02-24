<!-- Copyright © 2023 Samuel Justin Gabay
     Licensed under the GNU Affero Public License, Version 3 -->

<script setup lang="ts">
import { useSlots } from "vue";

// “full” is used for job experience so it renders the job title
const headerClass = useSlots().header!().length == 4 ? "full" : undefined;
</script>

<template>
  <section>
    <header :class="headerClass">
      <slot name="header" />
    </header>
    <slot />
  </section>
</template>

<style scoped>
section {
  margin: 1.5em 0;
}

h3 + section {
  margin: 1em 0;
}

header {
  margin-top: 1em;
  padding: 0;
  display: grid;
  grid-template-columns: 8.5em 1fr 8.5em;
  grid-template-areas: "time-frame organization location";
  gap: 0.25em 1em;
}

header.full {
  grid-template-areas:
    "time-frame organization location"
    "job-title  job-title    job-title";
}

@media (max-width: 28em) {
  header {
    grid-template-columns: 1fr 1fr;
    grid-template-areas:
      "time-frame   location"
      "organization organization";
  }

  header.full {
    grid-template-areas:
      "time-frame   location"
      "organization organization"
      "job-title    job-title";
  }
}

header :deep(h4) {
  margin: 0;
}

header :deep(:first-child) {
  grid-area: time-frame;
}

header :deep(:nth-child(2)) {
  text-align: center;
  grid-area: organization;
}

header :deep(:nth-child(3)) {
  text-align: right;
  grid-area: location;
}

header :deep(:nth-child(4)) {
  text-align: center;
  grid-area: job-title;
}

section :deep(ul) {
  padding-left: 2em;
  margin-top: 0.5em;
}
</style>
