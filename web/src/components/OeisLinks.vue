<script setup lang="ts">
import { OeisId } from "@/oeis";

const props = defineProps <{
  textOrId: string | OeisId;
}>();

function getParts() {
  if (typeof props.textOrId != "string") {
    return [props.textOrId];
  }

  let index = 0;
  const parts: (string | OeisId)[] = [];

  for (const match of props.textOrId.matchAll(/A0*[1-9]\d{0,8}/g)) {
    if (match.index! > 0) {
      parts.push(props.textOrId.substring(index, match.index!));
    }

    parts.push(OeisId.parse(match[0]));
    index += match.index! + match[0].length;
  }

  if (index < props.textOrId.length) {
    parts.push(props.textOrId.substring(index));
  }

  return parts;
};
</script>

<template>
  <template v-for="part in getParts()" :key="part">
    <template v-if="typeof part == 'string'">{{ part }}</template>
    <a v-else v-href="'https://oeis.org/' + part">{{ part }}</a>
  </template>
</template>
