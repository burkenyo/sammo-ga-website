import { fileURLToPath, URL } from "node:url";

import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import markdown from "vite-plugin-vue-markdown";
import pages, { type VueRoute } from 'vite-plugin-pages'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [
    vue({ include: [/\.vue$/, /\.md$/] }),

    markdown({
      markdownItSetup(md) {
        md.use(require("markdown-it-emoji"));
        // enable subscripts using ~X~
        md.use(require("markdown-it-sub"));
        // enable superscripts using ^X^
        md.use(require("markdown-it-sup"));
        md.use(require("markdown-it-link-attributes"), {
          // make external links open in new tabs
          matcher: (href: string) => !href.startsWith("/"),
          attrs: {
            target: "_blank",
          },
        });
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
});
