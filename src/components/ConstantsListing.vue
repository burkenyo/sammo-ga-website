<script setup lang="ts">
import InterestingConstant from "./InterestingConstant.vue";
import ConstIcons from "./icons/ConstIcons.vue";

const interestingConstants = [
  {
    tag: "pi",
    id: 796,
    description: "Pi, the ratio of a circle’s circumference to its diameter",
  },
  {
    tag: "e",
    id: 1113,
    description: "The base of the natural logarithm",
  },
  {
    tag: "gamma",
    id: 1620,
    description: "Gamma, the Euler-Mascheroni constant",
  },
  {
    tag: "phi",
    id: 1622,
    description: "Phi, the golden ratio",
  },
  {
    tag: "root_two",
    id: 2193,
    description: "The square-root of two",
  },
  {
    tag: "twelfth_root_two",
    id: 10774,
    description: "The ratio of the pitches of any two neighboring notes on the chromatic scale",
  },
  {
    tag: "tau",
    id: 19692,
    description: "Tau, two times pi, the ratio of a circle’s circumference to its radius",
  },
] as const;
</script>

<template>
  <InterestingConstant v-for="interestingConstant in interestingConstants" :key="interestingConstant.tag">
    <template #icon>
      <ConstIcons :tag="interestingConstant.tag" />
    </template>
    <template #heading>{{ interestingConstant.description }}</template>

    OEIS Sequence
    <a :href="'https://oeis.org/A' + interestingConstant.id" target="_blank">
      {{ interestingConstant.id }}
    </a>
  </InterestingConstant>
</template>
