<!-- Copyright © 2023 Samuel Justin Gabay
     Licensed under the GNU Affero Public License, Version 3 -->

<route>
{ meta: {
  title: "Projects",
  description: "A selection of Sammo’s current major projects"
} }
</route>

<script setup lang="ts">
import ConstantIcon from "@melodies/components/ConstantIcon.vue";
import ProjectInfo from "@main/components/ProjectInfo.vue";
import { localUrlForSubApp } from "@main/helpers";
</script>

<template>
  <h2>Selected Projects</h2>
  <section class="projects">
    <ProjectInfo title="Ranked Choice Voting" :href="localUrlForSubApp('/vote-demo')">
      Demo of a site I created to help my brass band select a new name, with a dashboard of plots
      and the ability to download anonymized ballots for hand-counting.
    </ProjectInfo>
    <ProjectInfo title="Cantera" href="https://cantera.org/" icon-src="/icons/cantera-logo-emblem-only.png">
      Developing a .NET interface for an open-source suite of tools targeting problems involving
      chemical kinetics, thermodynamics, and transport processes.
    </ProjectInfo>
    <ProjectInfo title="Mathematical Melodies" :href="localUrlForSubApp('/melodies')">
      Using the fractional expansion of numbers such as
      <ConstantIcon tag="pi"/> and <ConstantIcon tag="e"/> to generate musical melodies.
    </ProjectInfo>
    <ProjectInfo title="sammo.ga – This site!" href="/about">
      A labor of love (or necessity?) as I endeavored to learn front-end web technologies
      and build a web site from scratch.
    </ProjectInfo>
  </section>
</template>

<style scoped>
section.projects {
  display: flex;
  flex-wrap: wrap;
  justify-content: space-around;
  gap: 1em;
}
</style>
