<!-- Copyright © 2025 Samuel Justin Speth Gabay
     Licensed under the GNU Affero Public License, Version 3 -->

<route>
{ meta: {
  title: "Projects",
  description: "A selection of Sammo’s current major projects"
} }
</route>

<script setup lang="ts">
import { ref, onMounted } from "vue"
import { pickRandom, shuffle } from "@shared/utils"
import ConstantIcon from "@melodies/components/ConstantIcon.vue";
import ProjectInfo from "@main/components/ProjectInfo.vue";
import { localUrlForSubApp } from "@main/helpers";

const section = ref<HTMLElement | null>(null);

onMounted(() => {
  const possibleColors = ["green", "orange", "purple"];
  shuffle(possibleColors)

  const projectElements = section.value!.children;
  const fullGroups = projectElements.length / possibleColors.length | 0;
  const leftovers = projectElements.length % possibleColors.length;

  // randomize the colors of the project headers while ensuring there is always at least some of each color
  const colors = [];
  for (let i = 0; i < fullGroups; i++)
  {
    colors.push(...possibleColors);
  }
  for (let i = 0; i < leftovers; i++)
  {
    colors.push(possibleColors.pop() as string);
  }
  shuffle(colors);

  for (let i = 0; i < projectElements.length; i++)
  {
    projectElements[i]!.classList.add(colors[i]!);
  }
})

</script>

<template>
  <h2>Selected Projects</h2>
  <section class="projects" ref="section">
    <ProjectInfo title="IoT at .NET Conf 2023" href="https://www.youtube.com/watch?v=zwkspYxtFAE" icon-src="/icons/dotnet_bot.png">
      Smart home automation which leverages C# on resource-constrained devices: an ESP32-series microcontroller and a Raspberry Pi.
    </ProjectInfo>
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
    <ProjectInfo title="sammo.ga – This site" href="/about">
      I built this site from scratch using Vue with hand-written CSS. A workflow
      automatically integrates committed changes.
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
