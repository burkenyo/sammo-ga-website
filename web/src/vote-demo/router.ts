// Copyright © 2025 Samuel Justin Speth Gabay
// Licensed under the GNU Affero Public License, Version 3

import { createRouter, createWebHistory } from "vue-router";

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: "/vote-demo",
      redirect: { name: "vote" },
    },
    {
      path: "/vote-demo/vote",
      name: "vote",
      component: () => import("@vote-demo/pages/ElectionPage.vue"),
    },
    {
      path: "/vote-demo/results",
      name: "results",
      component: () => import("@vote-demo/pages/ResultsPage.vue"),
    },
  ],
});

export default router;
