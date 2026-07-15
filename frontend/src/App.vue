<script setup lang="ts">
import { useRoute } from "vue-router";
import { useAuth } from "./auth/useAuth";

const auth = useAuth();
const route = useRoute();

function signIn(): void {
  void auth.signIn(route.fullPath);
}
</script>

<template>
  <header class="site-header">
    <RouterLink to="/" class="brand">★ Product Reviews</RouterLink>
    <nav class="auth-area">
      <template v-if="auth.isSignedIn">
        <span class="user-name">{{ auth.displayName }}</span>
        <button type="button" class="button subtle" @click="auth.signOut()">Sign out</button>
      </template>
      <button v-else-if="auth.isAvailable" type="button" class="button" @click="signIn">Sign in</button>
    </nav>
  </header>

  <main class="page">
    <RouterView />
  </main>

  <footer class="site-footer">
    A showcase for
    <a href="https://github.com/KaliCZ/StrongTypes" target="_blank" rel="noopener">Kalicz.StrongTypes</a>
    — every constraint in this UI flows from the API's OpenAPI schema.
  </footer>
</template>
