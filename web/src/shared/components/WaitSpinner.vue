<template>
  <div />
</template>

<style scoped>
div {
  display: inline-block;
  width: 2em;
  height: 2em;
  vertical-align: -0.125em;
  border-radius: 50%;
  animation: 0.75s linear infinite spinner-border;
  border: 0.25em solid var(--blue-bold);
  border-right-color: transparent;
  margin: 1em;
}

@keyframes spinner-border {
  to {
    transform: rotate(360deg);
  }
}
</style>
