<script setup lang="ts">
defineProps<{
  /** 0–5, fractional allowed for averages; null renders as empty stars. */
  value: number | null;
  editable?: boolean;
}>();

const emit = defineEmits<{ select: [stars: number] }>();
</script>

<template>
  <span
    v-if="!editable"
    class="star-rating"
    role="img"
    :aria-label="value === null ? 'Not yet rated' : `Rated ${value.toFixed(1)} out of 5`"
  >
    ★★★★★
    <span class="stars-filled" :style="{ width: `${((value ?? 0) / 5) * 100}%` }" aria-hidden="true">★★★★★</span>
  </span>
  <span v-else class="star-rating editable">
    <button
      v-for="stars in 5"
      :key="stars"
      type="button"
      class="star-button"
      :class="{ filled: value !== null && stars <= value }"
      :aria-label="`${stars} star${stars > 1 ? 's' : ''}`"
      @click="emit('select', stars)"
    >
      ★
    </button>
  </span>
</template>
