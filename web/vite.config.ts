/// <reference types="./md-plugins" />

import { fileURLToPath, URL } from "node:url";
import { defineConfig, loadEnv } from "vite";
import vue from "@vitejs/plugin-vue";
import markdown from "vite-plugin-vue-markdown";
import pages, { type VueRoute } from "vite-plugin-pages";
import type { PluginHooks } from "rollup";

// markdown-it plugins
import mdEmoji from "markdown-it-emoji";
import mdSub from "markdown-it-sub";
import mdSup from "markdown-it-sup";
import mdLinkAttrs from "markdown-it-link-attributes";
import mdReplaceLink from "markdown-it-replace-link";
import mdImageFigures from "markdown-it-image-figures";

const ASSETS_BASE_URL = "VITE__ASSETS_BASE_URL";

// https://vitejs.dev/config/
export default defineConfig(({ command, mode, ssrBuild }) => {
  process.env.VITE__COMMAND = command;
  const env = loadEnv(mode, process.cwd());

  // ensure ssrBuild has a boolean value;
  ssrBuild = !!ssrBuild;

  return {
    plugins: [
      vue({ include: [/\.vue$/, /\.md$/] }),

      markdown({
        markdownItSetup(md) {
          md.use(mdEmoji);

          // enable subscripts using ~X~
          md.use(mdSub);

          // enable superscripts using ^X^
          md.use(mdSup);

          md.use(mdLinkAttrs, {
            // make external links open in new tabs
            matcher: (href: string) => !href.startsWith("/"),
            attrs: {
              target: "_blank",
            },
          });

          md.use(mdReplaceLink, {
            // inject the base URL for externally-stored assets
            replaceLink: (link: string) => link.startsWith(ASSETS_BASE_URL)
              ? env[ASSETS_BASE_URL] + link.substring(ASSETS_BASE_URL.length + 1)
              : link,
          });

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

function buildScript(options: { name: string; hook: keyof PluginHooks | "config"; func: () => Promise<any>; when?: boolean }) {
  return {
    name: options.name,

    apply: () => options.when == undefined || options.when,

    [options.hook]: async () => {
      console.log(`Running “${options.name}” build script...`);
      await options.func();
    },
  } as const;
}
