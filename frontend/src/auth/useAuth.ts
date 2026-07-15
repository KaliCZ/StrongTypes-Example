import { UserManager, type User } from "oidc-client-ts";
import { defineStore } from "pinia";

// Lazy so importing this module (e.g. from unit tests) never touches browser APIs
// or requires the OIDC env to be present.
let userManager: UserManager | null = null;

function getUserManager(): UserManager {
  userManager ??= new UserManager({
    authority: import.meta.env.VITE_OIDC_AUTHORITY ?? "",
    client_id: import.meta.env.VITE_OIDC_CLIENT_ID ?? "",
    redirect_uri: `${window.location.origin}/auth/callback`,
    post_logout_redirect_uri: window.location.origin,
    response_type: "code",
    scope: "openid profile email",
  });
  return userManager;
}

export const useAuth = defineStore("auth", {
  state: () => ({
    user: null as User | null,
    ready: false,
  }),
  getters: {
    // Sign-in is only offered when the AppHost provisioned an OIDC client.
    isAvailable: () => Boolean(import.meta.env.VITE_OIDC_AUTHORITY && import.meta.env.VITE_OIDC_CLIENT_ID),
    isSignedIn: (state) => state.user !== null && !state.user.expired,
    displayName: (state) => state.user?.profile.name ?? "",
    accessToken: (state) => (state.user !== null && !state.user.expired ? state.user.access_token : null),
  },
  actions: {
    async initialize() {
      if (this.isAvailable) {
        this.user = await getUserManager().getUser();
      }
      this.ready = true;
    },
    async signIn(returnTo?: string) {
      await getUserManager().signinRedirect({ state: returnTo ?? window.location.pathname });
    },
    /** Completes the redirect flow on /auth/callback; returns where to navigate next. */
    async completeSignIn(): Promise<string> {
      const user = await getUserManager().signinRedirectCallback();
      this.user = user;
      return typeof user.state === "string" && user.state.startsWith("/") ? user.state : "/";
    },
    async signOut() {
      this.user = null;
      await getUserManager().signoutRedirect();
    },
  },
});
