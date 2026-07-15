import { createPinia } from "pinia";
import { createApp } from "vue";
import App from "./App.vue";
import { setAuthTokenProvider } from "./api/client";
import { useAuth } from "./auth/useAuth";
import { router } from "./router";
import "./styles.css";

const app = createApp(App);
app.use(createPinia());
app.use(router);

const auth = useAuth();
setAuthTokenProvider(() => auth.accessToken);
void auth.initialize();

app.mount("#app");
