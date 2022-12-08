<script setup lang="ts">
import InterestingConstant from "./InterestingConstant.vue";
import ConstIcons from "./icons/ConstIcons.vue";

import { serviceKeys, useServices } from "@/services";

const interestingConstantsService = useServices().retrieve(serviceKeys.interestingConstants);
const interestingConstants = await Promise.all(interestingConstantsService.get());
</script>

<template>
  <InterestingConstant v-for="interestingConstant in interestingConstants" :key="interestingConstant.tag">
    <template #icon>
      <ConstIcons :tag="interestingConstant.tag" />
    </template>
    <template #heading>{{ interestingConstant.description }}</template>

    OEIS Sequence
    <a :href="'https://oeis.org/' + interestingConstant.expansion.id" target="_blank">
      {{ interestingConstant.expansion.id }}
    </a>
    {{ String(interestingConstant.expansion.expansion).substring(0, 80) }}…
  </InterestingConstant>
</template>
