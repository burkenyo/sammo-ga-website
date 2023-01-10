<script setup lang="ts">
const props = defineProps<{
  title: string;
  href: string;
  iconSrc?: string;
}>();

const styleForIcon = props.iconSrc
  ? {
    backgroundImage: `url(${props.iconSrc}`,
  }
  : undefined;
</script>

<template>
  <a :href="href" :target="href.startsWith('/') ? undefined : '_blank'">
    <h3 :style="styleForIcon">{{ title }}</h3>
    <p><slot /></p>
  </a>
</template>

<style scoped>
a {
  padding: 0.5em;
  display: block;
  width: 14em;
  height: 10em;
  border-radius: 0.2em;
  text-decoration: none;
  color: black;
  box-shadow: 0 0 4px var(--grey-darker);
  transition: box-shadow 200ms;
}

a:hover {
  box-shadow: 0 0 4px var(--blue-lighter);
  transition: box-shadow 200ms;
}

a:nth-of-type(3n + 1) {
  border-top: 4px solid var(--orange);
}

a:nth-of-type(3n + 2) {
  border-top: 4px solid var(--green);
}

a:nth-of-type(3n) {
  border-top: 4px solid var(--purple-lighter);
}

h3 {
  margin-top: 0;
  border: none;
  background-size: contain;
  background-repeat: no-repeat;
  background-position: right;
}

p {
  margin: 0;
}
</style>
