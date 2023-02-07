// this reference enables “import routes from "~pages"” to pass type checking
/// <reference types="vite-plugin-pages/client" />

import { createPinia } from "pinia";
import App from "@/App.vue";
import "./assets/main.css";
import "./assets/utils.css";
import routes from "~pages";
import { ViteSSG } from "vite-ssg";
import type { App as VueApp, DirectiveBinding } from "vue";

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
// Static URLs can use a plain href attribute; it will be fixed up by the NodeTransform function
// provided in vite.config.ts
function setLinkTargets(app: VueApp) {
  function setLinkTarget(el: unknown, binding: DirectiveBinding<unknown>) {
    if (!(el instanceof HTMLAnchorElement) || !binding.value) {
      return;
    }

    const href = String(binding.value);
    el.href = href;

    if (href.startsWith("http")) {
      el.target = "_blank";
    }
  }

  app.directive("href", {
    mounted: setLinkTarget,
    updated: setLinkTarget,

    getSSRProps(binding) {
      if (!binding.value) {
        return;
      }

      const href = String(binding.value);

      if (!href.startsWith("http")) {
        return { href };
      }

      return { href, target: "_blank" };
    },
  });
}
