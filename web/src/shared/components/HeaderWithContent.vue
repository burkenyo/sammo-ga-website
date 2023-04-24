<!-- Copyright © 2023 Samuel Justin Gabay
     Licensed under the GNU Affero Public License, Version 3 -->

<template>
  <header>
    <slot />
  </header>
</template>

<style scoped>
header {
  margin: 1.2em 0;
  border-bottom: 2px solid;
  border-image: linear-gradient(90deg, var(--blue) 0%, var(--blue-lighter) 100%) 1;
}

header :deep(h2) {
  margin: 0 1em 0 0;
  display: inline;
  border: none;
}

header :deep(a:hover) {
  text-decoration: underline solid transparent 1px;
}
</style>
