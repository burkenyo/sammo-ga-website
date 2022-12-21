<script setup lang="ts">
import { OeisId } from '@/oeis';

const props = defineProps<{
  text: OeisId | string;
}>();

function getParts() {
  if (typeof props.text != 'string') {
    return [props.text];
  }

  let index = 0;
  const parts: (string | OeisId)[] = [];

  for (const match of props.text.matchAll(/A0*[1-9]\d{0,8}/g)) {
    if (match.index! > 0) {
      parts.push(props.text.substring(index, match.index!));
    }

    parts.push(OeisId.parse(match[0]));
    index += match.index! + match[0].length;
  }

  if (index < props.text.length) {
    parts.push(props.text.substring(index));
  }

  return parts;
};
</script>

<template>
  <template v-for="part in getParts()" :key="part">
    <template v-if="typeof part == 'string'">{{ part }}</template>
    <a v-else :href="'https://oeis.org/' + part" target="_blank">{{ part }}</a>
  </template>
</template>
