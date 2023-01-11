import { createPinia } from "pinia";
import App from "@/App.vue";
import "./assets/main.css";
import "./assets/utils.css";
import routes from "~pages";
import { ViteSSG } from 'vite-ssg'

export const createApp = ViteSSG(
  // root component
  App,

  // vue-router options
  { routes },

  // Vue setup
  ({ app }) => {
    app.use(createPinia());
  }
);
