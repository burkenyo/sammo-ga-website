// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

import { createApp } from "vue";
import App from "./App.vue";
import router from "./router";
import { createPinia } from "pinia";
import "./assets/bootstrap-augments.css";
import "./assets/colors.css";

const app = createApp(App);

app.use(router);
app.use(createPinia());

app.mount("#app");
