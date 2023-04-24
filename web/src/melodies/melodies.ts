// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

import { createPinia } from "pinia";
import "@shared/assets/main.css";
import "@shared/assets/utils.css";
import { createApp } from "vue";
import SimpleApp from "@shared/SimpleApp.vue";
import MelodiesPage from "./pages/MelodiesPage.vue";

const app = createApp(SimpleApp, {
  heading: "Mathematical Melodies",
  root: MelodiesPage,
});

app.use(createPinia());

app.mount("#app");
