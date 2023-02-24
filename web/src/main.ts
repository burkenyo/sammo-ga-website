// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

// this reference enables “import routes from "~pages"” to pass type checking
/// <reference types="vite-plugin-pages/client" />

import { createPinia } from "pinia";
import App from "@/App.vue";
import "./assets/main.css";
import "./assets/utils.css";
import routes from "~pages";
import { ViteSSG } from "vite-ssg";
import type { App as VueApp, DirectiveBinding } from "vue";
import { getLinkProps } from "./vue-utils";

export const createApp = ViteSSG(
  // root component
  App,

  // vue-router options
  { routes },

  // Vue setup
  ({ app }) => {
    app.use(createPinia());
    app.use(setLinkTargets);
  }
);

// mini-plugin to automatically set target="_blank" when needed for dynamic urls, i.e. from props, refs, etc.
// Provides a v-href directive that will add a target="_blank" attribute for external links.
// Static URLs used in templates can use a plain href attribute; it will be fixed up by the NodeTransform function
// provided in vite.config.ts
function setLinkTargets(app: VueApp) {
  function setLinkTarget(el: unknown, binding: DirectiveBinding<unknown>) {
    if (!(el instanceof HTMLAnchorElement) || !binding.value) {
      return;
    }

    Object.assign(el, getLinkProps(String(binding.value)));
  }

  app.directive("href", {
    mounted: setLinkTarget,
    updated: setLinkTarget,

    getSSRProps(binding) {
      if (!binding.value) {
        return;
      }

      return getLinkProps(String(binding.value));
    },
  });
}
