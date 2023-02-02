<template>
  <header>
    <h2><slot name="heading" /></h2>
    <slot />
  </header>
</template>

<style scoped>
header {
  margin: 1em 0;
  border-bottom: 2px solid;
  border-image: linear-gradient(90deg, var(--blue) 0%, var(--blue-lighter) 100%) 1;
}

header h2 {
  margin-right: 1em;
  display: inline;
  border: none;
}

header :deep(a:hover) {
  text-decoration: underline solid transparent 1px;
}
</style>
