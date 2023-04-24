// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

// this reference enables “import routes from "~pages"” to pass type checking
/// <reference types="vite-plugin-pages/client" />

import App from "@main/App.vue";
import "@shared/assets/main.css";
import "@shared/assets/utils.css";
import routes from "~pages";
import { ViteSSG } from "vite-ssg";
import { setLinkTargets } from "@shared/plugins";

export const createApp = ViteSSG(
  // root component
  App,

  // vue-router options
  { routes: routes },

  // Vue setup
  ({ app }) => {
    app.use(setLinkTargets);
  }
);
