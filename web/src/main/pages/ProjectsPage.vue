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
</script>

<template>
  <h2>Selected Projects</h2>
  <section class="projects">
    <ProjectInfo title="Mathematical Melodies" href="/melodies">
      Using the fractional expansion of numbers such as
      <ConstantIcon tag="pi"/> and <ConstantIcon tag="e"/> to generate musical melodies.
    </ProjectInfo>
    <ProjectInfo title="Cantera" href="https://cantera.org/" icon-src="/icons/cantera-logo-emblem-only.png">
      Developing a .NET interface for an open-source suite of tools targeting problems involving
      chemical kinetics, thermodynamics, and transport processes.
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
