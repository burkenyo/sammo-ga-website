// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

// mini-plugin to automatically set target="_blank" when needed for dynamic urls, i.e. from props, refs, etc.
// Provides a v-href directive that will add a target="_blank" attribute for external links.
// Static URLs used in templates can use a plain href attribute; it will be fixed up by the NodeTransform function

import type { DirectiveBinding, App } from "vue";
import { getLinkProps } from "./dom-utils";

// provided in vite.config.ts
export function setLinkTargets(app: App) {
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
