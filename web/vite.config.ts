import { fileURLToPath, URL } from "node:url";

import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import markdown from "vite-plugin-vue-markdown";

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [
    vue({ include: [/\.vue$/, /\.md$/] }),
    markdown({
      markdownItSetup(md) {
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
