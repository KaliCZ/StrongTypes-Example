<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { useAuth } from "../auth/useAuth";

const auth = useAuth();
const router = useRouter();
const failed = ref(false);

onMounted(async () => {
  try {
    const returnTo = await auth.completeSignIn();
    await router.replace(returnTo);
  } catch {
    failed.value = true;
  }
});
</script>

<template>
  <p v-if="failed" class="error-banner">
    Sign-in could not be completed. <RouterLink to="/">Back to the catalog</RouterLink>
  </p>
  <p v-else class="empty-state">Completing sign-in…</p>
</template>
