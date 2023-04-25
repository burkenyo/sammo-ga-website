<!-- Copyright © 2023 Samuel Justin Gabay
     Licensed under the GNU Affero Public License, Version 3 -->

<script setup lang="ts">
import { RouterLink, RouterView, useRouter } from "vue-router";
import { useElection } from "./election";
const election = useElection();

const router = useRouter();
</script>

<template>
  <header class="mb-4" style="background-color: #e3f2fd">
    <nav class="mt-0 navbar navbar-expand-sm navbar-light justify-content-center">
      <span class="navbar-brand">Ranked Choice Voting Demo</span>
      <ul class="navbar-nav">
        <li class="nav-item">
          <RouterLink :to="{ name: 'vote' }" class="nav-link">Vote</RouterLink>
        </li>
        <li class="nav-item">
          <RouterLink :to="{ name: 'results' }" class="nav-link">Results</RouterLink>
        </li>
        <li class="nav-item">
          <a @click="election.clear(); router.go(0);" href="#" class="nav-link">Clear</a>
        </li>
        <li class="nav-item">
          <a @click="election.reset(); router.go(0);" href="#" class="nav-link">Reset</a>
        </li>
        <li class="nav-item">
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
  line-height: 1.5;
  max-height: 100vh;
}

.logo {
  display: block;
  margin: 0 auto 2rem;
}

nav {
  width: 100%;
  font-size: 12px;
  text-align: center;
  margin-top: 2rem;
}

nav a.router-link-exact-active {
  color: var(--color-text);
}

nav a.router-link-exact-active:hover {
  background-color: transparent;
}

nav a {
  display: inline-block;
  padding: 0 1rem;
  border-left: 1px solid var(--color-border);
}

nav a:first-of-type {
  border: 0;
}

@media (min-width: 1024px) {
  header {
    display: flex;
    place-items: center;
    padding-right: calc(var(--section-gap) / 2);
  }

  .logo {
    margin: 0 2rem 0 0;
  }

  header .wrapper {
    display: flex;
    place-items: flex-start;
    flex-wrap: wrap;
  }

  nav {
    text-align: left;
    margin-left: -1rem;
    font-size: 1rem;

    padding: 1rem 0;
    margin-top: 1rem;
  }
}
</style>
