<script setup lang="ts">
import { onMounted, ref } from "vue";
import { api, type ProductSummary } from "../api/client";
import StarRating from "../components/StarRating.vue";

const products = ref<ProductSummary[]>([]);
const loading = ref(true);
const loadFailed = ref(false);

onMounted(async () => {
  const { data, error } = await api.GET("/api/products");
  loading.value = false;
  if (error !== undefined || data === undefined) {
    loadFailed.value = true;
    return;
  }
  products.value = data;
});
</script>

<template>
  <p v-if="loading" class="empty-state">Loading the catalog…</p>
  <p v-else-if="loadFailed" class="error-banner">The catalog could not be loaded. Is the API running?</p>
  <div v-else class="catalog-grid">
    <RouterLink
      v-for="product in products"
      :key="product.id"
      :to="{ name: 'product', params: { slug: product.slug } }"
      class="product-card"
    >
      <img v-if="product.imageUrl" :src="product.imageUrl" :alt="product.name" loading="lazy" />
      <div class="product-card-body">
        <h2>{{ product.name }}</h2>
        <p class="rating-line">
          <StarRating :value="product.averageRating ?? null" />
          <template v-if="product.averageRating != null">
            {{ product.averageRating.toFixed(1) }} · {{ product.reviewCount }}
            review{{ product.reviewCount === 1 ? "" : "s" }}
          </template>
          <template v-else>Not yet rated</template>
        </p>
      </div>
    </RouterLink>
  </div>
</template>
