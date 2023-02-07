/// <reference types="./md-plugins" />
/// <reference types="./env" />

import { fileURLToPath, URL } from "node:url";
import { defineConfig, loadEnv } from "vite";
import vue from "@vitejs/plugin-vue";
import markdown from "vite-plugin-vue-markdown";
import pages, { type VueRoute } from "vite-plugin-pages";
import type { PluginHooks } from "rollup";
import replace from "@rollup/plugin-replace";
import type { RootNode, TemplateChildNode, AttributeNode } from "@vue/compiler-core";

// markdown-it plugins
import mdEmoji from "markdown-it-emoji";
import mdSub from "markdown-it-sub";
import mdSup from "markdown-it-sup";
import mdAnchor from "markdown-it-anchor";
import mdImageFigures from "markdown-it-image-figures";

// https://vitejs.dev/config/
export default defineConfig(({ command, mode, ssrBuild }) => {
  process.env.VITE__COMMAND = command;
  const env = loadEnv(mode, process.cwd()) as ImportMetaEnv;

  // ensure ssrBuild has a boolean value;
  ssrBuild = !!ssrBuild;

  return {
    plugins: [
      replace({
        include: ["**.vue", "**.md"],
        __ASSETS_BASE_URL: env.VITE__ASSETS_BASE_URL?.replace(/\/?$/, ""),
      }),

      vue({
        include: ["**.vue", "**.md"],
        template: {
          compilerOptions: { nodeTransforms: [setLinkTarget] },
        },
      }),

      markdown({
        markdownItSetup(md) {
          md.use(mdEmoji);

          // enable subscripts using ~X~
          md.use(mdSub);

          // enable superscripts using ^X^
          md.use(mdSup);

          // automatically generate IDs for h3’s
          md.use(mdAnchor, { tabIndex: false, level: [3] });

          // place images inside figures
          md.use(mdImageFigures);
        },
      }),

      pages({
        extensions: ["vue", "md"],
        onRoutesGenerated: (routes: VueRoute[]) =>
          routes.map(r => ({
            ...r,
            path: r.path.replace(/(index)?page$/, ""),
          })),
      }),

      // setup mock services when testing locally
      buildScript({
        name: "scaffold-mocks",
        hook: "buildStart",
        func: () => import("./scripts/scaffold-mocks"),
        when: command == "serve",
      }),

      // clean-up output directory when building
      buildScript({
        name: "build-helper",
        hook: "closeBundle",
        func: () => import("./scripts/build-helper"),
        when: ssrBuild,
      }),

      // populate git-related environment variables
      buildScript({
        name: "get-git-info",
        hook: "config",
        func: () => import("./scripts/get-git-info"),
      }),
    ],

    resolve: {
      alias: {
        "@": fileURLToPath(new URL("./src", import.meta.url)),
      },
    },

    esbuild: {
      drop: ["debugger"],
      pure: ["console.debug", "console.trace"],
    },
  };
});

function buildScript(
  options: { name: string; hook: keyof PluginHooks | "config"; func: () => Promise<unknown>; when?: boolean }
) {
  return {
    name: options.name,

    apply: () => options.when == undefined || options.when,

    [options.hook]: async () => {
      console.log(`Running “${options.name}” build script...`);
      await options.func();
    },
  } as const;
}

// NodeTransform function to add target="_blank" attribute for external links
// This only works for links with a static href attribute.
// Dynamic URLs are supported via the v-href directive provided in src/main.ts
function setLinkTarget(node: RootNode | TemplateChildNode) {
  if (!("tag" in node) || node.tag != "a") {
    return;
  }
  const href = node.props.find(n => n.name == "href") as AttributeNode | undefined;

  if (!href?.value?.content.startsWith("http")) {
    return;
  }

  node.props.push({
    type: href.type,
    name: "target",
    value: {
      type: href.value.type,
      content: "_blank",
      loc: href.value.loc,
    },
    loc: href.loc,
  });
}
