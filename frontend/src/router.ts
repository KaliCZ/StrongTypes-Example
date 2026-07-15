import { createRouter, createWebHistory } from "vue-router";
import AuthCallbackPage from "./pages/AuthCallbackPage.vue";
import CatalogPage from "./pages/CatalogPage.vue";
import ProductDetailPage from "./pages/ProductDetailPage.vue";

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: "/", name: "catalog", component: CatalogPage },
    { path: "/products/:slug", name: "product", component: ProductDetailPage, props: true },
    { path: "/auth/callback", name: "auth-callback", component: AuthCallbackPage },
  ],
});
