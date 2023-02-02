<template>
  <section>
    <div class="icons">
      <slot name="icons" />
    </div>
    <div class="skills">
      <slot />
    </div>
  </section>
</template>

<style scoped>
section {
  display: flex;
  gap: 0 1.5em;
  flex-wrap: wrap;
  align-items: center;
}

section :deep(h4) {
  margin-top: 0;
}

section :deep(p) {
  margin-bottom: 0;
}

/* the min-width is based around the length of the header */
@media (max-width: 45em) {
  div.skills {
    flex-basis: 100%;
  }

  section + section {
    margin-top: 0.75em;
  }
}

div.icons {
  display: grid;
  grid-template-columns: repeat(5, 2.5em);
  grid-template-rows: 2.5em;
  gap: 0.75em;
  margin: 1em;
  justify-items: center;
  align-items: center;
}

div.icons :deep(img) {
  max-width: 100%;
  max-height: 100%;
}
</style>
