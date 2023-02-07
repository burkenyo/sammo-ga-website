<route>
{ meta: {
  title: "Resume",
  description: "Chronicles Sammo’s skills and work experience",
  menuOrder: 1
} }
</route>

<script setup lang="ts">
import SkillsSection from "@/components/SkillsSection.vue";
import HeaderWithContent from "@/components/HeaderWithContent.vue";
import ExperienceSection from "@/components/ExperienceSection.vue";
</script>

<HeaderWithContent>
<template #heading>Resume</template>
<a href="__ASSETS_BASE_URL/SGabay_Resume.pdf" target="_blank">Download a copy</a>
</HeaderWithContent>

Years of integration experience and back-end programming in multiple languages have honed my ability to create scalable and flexible systems. I have recently undertaken to massively scale my front-end knowledge as well. I seek opportunities to apply my aptitude and insatiable appetite for learning new technologies to the world where code meets art.

<section>

### Skills

<SkillsSection>
<template #icons>
  <img src="/icons/C_Sharp_wordmark.svg" />
  <img src="/icons/Python-logo-notext.svg" />
  <img src="/icons/ts-logo-128.svg" />
  <img src="/icons/Java_programming_language_logo.svg" />
</template>

#### C# • Python • TypeScript • Java

A diverse set of languages
</SkillsSection>

<SkillsSection>
<template #icons>
  <img src="/icons/Git-Icon-1788C.svg" />
  <img src="/icons/gh-actions.png" />
  <img src="/icons/Moby-logo.png" />
  <img src="/icons/k8s-logo.svg" />
</template>

#### Git • CI/CD • Docker • Kubernetes

Portable code that runs anywhere
</SkillsSection>

<SkillsSection>
<template #icons>
  <img src="/icons/logo_ASP.NET_RGB_square-negative.svg" />
  <img src="/icons/Vue-logo.svg" />
  <img src="/icons/Azure-logo.svg" />
</template>

#### ASP.NET Core • Vue • Azure

Multiple web frameworks with cloud experience
</SkillsSection>

<SkillsSection>
<template #icons>
  <img src="/icons/sql.svg" />
  <img src="/icons/PostgreSQL_logo.3colors.svg" />
  <img src="/icons/s3-logo.png" />
  <img src="/icons/Swagger-logo.png" />
</template>

#### SQL Server • PostgreSQL • S3/Blobs • Web APIs

Various data storage and retrieval paradigms
</SkillsSection>

<SkillsSection>
<template #icons>
  <img src="/icons/Tux.svg" />
  <img src="/icons/Windows-logo.svg" />
  <img src="/icons/Finder_icon.png" />
  <img src="/icons/bash_logo.svg" />
  <img src="/icons/ps_black_128.svg" />
</template>

#### Linux • Windows • macOS • Bash/Zsh • PowerShell

Dexterity across desktop and server OS’s
</SkillsSection>
</section>

<section>

### Experience

<ExperienceSection>
<template #header>

  #### 2022 – present

  <a href="https://cantera.org/" target="_blank">

  #### Cantera
  </a>

  #### Somerville, MA
  #### C#/Python specialist
</template>

* Introduce a new interface to consume a complicated scientific computing toolset from .NET by leveraging high-performance, low-level interop techniques.
* Develop a Python-based source generator to automate scaffolding C# code from the underlying C API
</ExperienceSection>

<ExperienceSection>
<template #header>

  #### 2022
  <a href="https://www.rockstargames.com/" target="_blank">

  #### Rockstar Games
  </a>

  #### Andover, MA
  #### Software Engineer
</template>

* Developed and maintained the online services platform with over 100 million distinct users and 1 million
concurrently online players
* Provided operational support for title releases/updates, minimizing down-time during high usage periods
* Fostered a culture of quality by writing unit tests for each new piece of code, performing stringent code
reviews daily (including as approver), and submitting code changes only through our CI/CD pipeline
* Adhered to a stringent set of coding standards while championing modernization
* Innovated new services in coordination with the game engine team to provide more features for players
</ExperienceSection>

<ExperienceSection>
<template #header>

  #### 2017 – 2022
  <a href="https://www.wgbh.org" target="_blank">

  #### WGBH Educational Foundation
  </a>

  #### Boston, MA
  #### Senior Software Developer/Integrations Architect
</template>

* Developed C# and Python-based integration system of web APIs and microservices for billing, new hires, storage, mass data transform/copy, and systems monitoring
*  Introduced loosely coupled design to the foundation: one API for integrating several related applications
* Wrote add-ons for Jira and Confluence in Java and Groovy using a shared, reusable component design
* Implemented a complete CI/CD solution using the Atlassian Suite with automated build and deployment
* Authored comprehensive naming conventions and style guidelines for the systems department
</ExperienceSection>

<ExperienceSection>
<template #header>

  #### 2010 – 2014
  <a href="https://web.archive.org/web/20141022145017/http://www.emcore.com/" target="_blank">

  #### Emcore Corporation
  </a>

  #### Albuquerque, NM
  #### Senior Applications Developer/Consultant
</template>

* Developed and maintained .NET-based custom shop floor control system for the solar cell fab
* Slashed resource usage and loading time of the shop floor front-end by 80% and 40%
* Assisted with data extraction and migration using SQL for upgrading legacy systems
* Generated data packages to support external customer requests and resolve process excursions
* Released and documented software in compliance with industry-standard certification requirements
</ExperienceSection>

<ExperienceSection>
<template #header>

  #### 2008 – 2010
  <a href="https://web.archive.org/web/20141006023739/http://www.emcore.com/space-photovoltaics/" target="_blank">

  #### Emcore Solar Power, Inc.
  </a>

  #### Albuquerque, NM
  #### Production Engineering Lead
</template>

* Developed, sustained, and optimized processes to improve yield and throughput while trimming costs
* Doubled diode assembly production capacity
* Reduced CIC and diode assembly shipping costs by 30% with automated reporting and labeling
</ExperienceSection>
</section>

<section>

### Education

<ExperienceSection>
<template #header>

  #### 2015 – 2016
  <a href="https://necmusic.edu/" target="_blank">

  #### New England Conservatory
  </a>

  #### Boston, MA
</template>

* Composition Studies with Kati Agócs, Conducting/Rehearsal Studies with Michael Gandolfi
* Theory Department Tutor for Tonal Practice Harmony
</ExperienceSection>

<ExperienceSection>
<template #header>

  #### 2012 – 2015
  <a href="https://longy.edu/" target="_blank">

  #### Longy School of Music
  </a>

  #### Cambridge, MA
</template>

* Composition Studies with Paul Brust, Piano Studies with Libor Dudas
* Composition and Theory Department Tutor for Harmony and Keyboard Skills
</ExperienceSection>

<ExperienceSection>
<template #header>

  #### 2002 – 2007
  <a href="https://www.nmt.edu/" target="_blank">

  #### New Mexico Institute of Mining and Technology
  </a>

  #### Socorro, NM
</template>

* B.S. Materials Engineering, Minor in History
* Initiated into Tau Beta Pi, the engineering honors society
* Active member of student association, student materials science society, and intramural sports teams
</ExperienceSection>
</section>
