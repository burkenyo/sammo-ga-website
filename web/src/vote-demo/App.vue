<!-- Copyright © 2023 Samuel Justin Gabay
     Licensed under the GNU Affero Public License, Version 3 -->

<template>
  <header class="py-2 mb-4">
    <nav class="mt-0 navbar navbar-expand-sm navbar-dark justify-content-center">
      <span class="navbar-brand">Ranked Choice Voting Demo</span>
      <ul class="navbar-nav">
        <li class="nav-item" tabindex="0">
          <RouterLink :to="{ name: 'vote' }" class="nav-link">Vote</RouterLink>
        </li>
        <li class="nav-item" tabindex="0">
          <RouterLink :to="{ name: 'results' }" class="nav-link">Results</RouterLink>
        </li>
        <li class="nav-item" tabindex="0">
          <a href="/" class="nav-link">Home</a>
        </li>
      </ul>
    </nav>
  </header>
  <div class="container">
    <main role="main" class="pb-3 text-center">
      <RouterView />
    </main>
  </div>
</template>

<style scoped>
header {
  background-color: var(--color-main);
}

nav a.router-link-active {
  color: white;
}

nav a:link {
  text-decoration: underline solid transparent;
  transition: text-decoration 100ms;
}

nav a:hover {
  text-decoration: underline;
  transition: text-decoration 100ms;
}
</style>
