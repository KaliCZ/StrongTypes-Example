/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** OIDC issuer (Zitadel), injected by the Aspire AppHost. Empty when auth is unavailable. */
  readonly VITE_OIDC_AUTHORITY?: string;
  /** The provisioned PKCE SPA client id, injected by the Aspire AppHost. */
  readonly VITE_OIDC_CLIENT_ID?: string;
}

declare module "*.vue" {
  import type { DefineComponent } from "vue";
  const component: DefineComponent<Record<string, never>, Record<string, never>, unknown>;
  export default component;
}
