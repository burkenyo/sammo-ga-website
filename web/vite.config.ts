// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

/// <reference types="vitest" />
/// <reference types="./md-plugins" />
/// <reference types="./env" />

import { defineConfig, loadEnv } from "vite";
import vue from "@vitejs/plugin-vue";
import markdown from "unplugin-vue-markdown/vite";
import pages, { type VueRoute } from "vite-plugin-pages";
import type { PluginHooks } from "rollup";
import replace from "@rollup/plugin-replace";
import type { RootNode, TemplateChildNode, AttributeNode } from "@vue/compiler-core";
import path from "node:path";

// markdown-it plugins
import mdEmoji from "markdown-it-emoji";
import mdSub from "markdown-it-sub";
import mdSup from "markdown-it-sup";
import mdAnchor from "markdown-it-anchor";
import mdImageFigures from "markdown-it-image-figures";
import type { Plugin } from "vite";

// https://vitejs.dev/config/
export default defineConfig(({ command, mode, isSsrBuild }) => {
  process.env.VITE__COMMAND = command;
  const env = loadEnv(mode, process.cwd()) as ImportMetaEnv;

  // ensure ssrBuild has a boolean value;
  isSsrBuild = !!isSsrBuild;

  return {
    root: resolve("src"),
    publicDir: resolve("public"),
    envDir: __dirname,

    define: {
      __VUE_OPTIONS_API__: false,
    },

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
        dirs: resolve("src/main/pages"),
      }),

      // Rewrites for the vote-demo app, must be synced with src/vote-demo/router.ts
      rewriteSubAppPages("vote-demo", ["vote", "results", "success"]),

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
        when: isSsrBuild,
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
        "@main": resolve("src/main"),
        "@shared": resolve("src/shared"),
        "@melodies": resolve("src/melodies"),
        "@vote-demo": resolve("src/vote-demo"),
      },
    },

    esbuild: {
      drop: ["debugger"],
      pure: ["console.debug", "console.trace"],
    },

    build: {
      outDir: resolve("dist"),

      rollupOptions: {
        input: {
          main: resolve("src/index.html"),
          melodies: resolve("src/melodies/index.html"),
          "vote-demo": resolve("src/vote-demo/index.html"),
        },
      },
    },

    test: {
      root: resolve("test"),
    },
  };
});

interface BuildScriptOptions {
  readonly name: string;
  readonly hook: keyof PluginHooks | "config";
  readonly func: () => Promise<unknown>;
  readonly when?: boolean;
}

// execute an arbitrary script during the build process
function buildScript(options: BuildScriptOptions): Plugin {
  return {
    name: options.name,

    apply: () => options.when == undefined || options.when,

    [options.hook]: async () => {
      console.log(`Running “${options.name}” build script...`);
      await options.func();
    },
  } as const;
}

// Rewrite a sub-app’s known pages to the default route for that app.
// The app’s router should kick in client-side and render the correct content.
function rewriteSubAppPages(subApp: string, pages: string[]): Plugin {
  return {
    name: subApp + "-rewrite",

    configureServer(serve) {
      serve.middlewares.use((req, _, next) => {
        if (pages.some(p => req.url?.startsWith("/" + subApp + "/" + p))) {
          req.url = "/" + subApp + "/";
        }

        next();
      });
    },
  };
}

// NodeTransform function to add target="_blank" attribute for external links
// This only works for links with a static href attribute.
// Dynamic URLs are supported via the v-href directive provided in src/main.ts
function setLinkTarget(node: RootNode | TemplateChildNode): void {
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

function resolve(relativePath: string): string {
  return path.resolve(__dirname, relativePath);
}
