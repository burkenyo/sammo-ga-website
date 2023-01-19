<route>
{ meta: {
  title: "Projects",
  description: "A selection of Sammo’s current major projects",
  menuOrder: 2
} }
</route>

<script setup lang="ts">
import ConstantIcon from "@/components/ConstantIcon.vue";
import ProjectInfo from "@/components/ProjectInfo.vue";
</script>

<template>
  <h2>Selected Projects</h2>
  <section class="projects">
    <ProjectInfo title="Mathematical Melodies" href="/melodies">
      Using the fractional expansion of numbers such as
      <ConstantIcon tag="pi"/> and <ConstantIcon tag="e"/> to generate musical melodies.
    </ProjectInfo>
    <ProjectInfo title="Cantera" href="https://cantera.org/" icon-src="/cantera-logo-emblem-only.png">
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
