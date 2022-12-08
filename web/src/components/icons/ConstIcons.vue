<script setup lang="ts">
import IconE from './IconE.vue';
import IconGamma from './IconGamma.vue';
import IconPhi from './IconPhi.vue';
import IconPi from './IconPi.vue';
import IconRootTwo from './IconRootTwo.vue';
import IconTau from './IconTau.vue';
import IconTwelfthRootTwo from './IconTwelfthRootTwo.vue';

defineProps<{
  tag: string;
}>();
</script>

<template>
  <IconE v-if="tag == 'e'" />
  <IconGamma v-if="tag == 'gamma'" />
  <IconPhi v-if="tag == 'phi'" />
  <IconPi v-if="tag == 'pi'" />
  <IconRootTwo v-if="tag == 'root_two'" />
  <IconTau v-if="tag == 'tau'" />
  <IconTwelfthRootTwo v-if="tag == 'twelfth_root_two'" />
</template>
