import { createApp } from "vue";
import { createPinia } from "pinia";
import App from "@/App.vue";
import "./assets/main.css";
import "./assets/utils.css";
import { createRouter, createWebHistory } from 'vue-router'
import routes from "~pages";

const router = createRouter({
  history: createWebHistory(),
  routes,
});

const app = createApp(App);

app.use(createPinia());
app.use(router);

app.mount("#app");
