<script setup lang="ts">
import { reactive } from "vue";
import type { Review } from "../api/client";
import type { ReviewFormValues } from "../api/editReviewBody";
import StarRating from "./StarRating.vue";

const props = defineProps<{
  mode: "create" | "edit";
  initial?: Review;
  busy?: boolean;
  error?: string | null;
}>();

const emit = defineEmits<{
  submit: [values: ReviewFormValues];
  cancel: [];
}>();

const values = reactive<ReviewFormValues>({
  rating: props.initial?.rating ?? 0,
  title: props.initial?.title ?? "",
  body: props.initial?.body ?? "",
  pros: props.initial?.pros ?? "",
  cons: props.initial?.cons ?? "",
});

function submit(): void {
  emit("submit", { ...values });
}
</script>

<template>
  <form class="review-form" @submit.prevent="submit">
    <h3>{{ mode === "create" ? "Write a review" : "Edit your review" }}</h3>

    <label>
      Rating
      <StarRating :value="values.rating || null" editable @select="(stars) => (values.rating = stars)" />
    </label>

    <label>
      Title
      <input v-model="values.title" required maxlength="200" placeholder="Sum it up in one line" />
    </label>

    <label>
      Review
      <textarea v-model="values.body" required maxlength="4000" placeholder="What worked, what didn't?"></textarea>
    </label>

    <label>
      Pros <span class="field-hint">(optional{{ mode === "edit" ? " — clear the field to remove" : "" }})</span>
      <input v-model="values.pros" maxlength="500" placeholder="The best thing about it" />
    </label>

    <label>
      Cons <span class="field-hint">(optional{{ mode === "edit" ? " — clear the field to remove" : "" }})</span>
      <input v-model="values.cons" maxlength="500" placeholder="The worst thing about it" />
    </label>

    <p v-if="error" class="error-banner">{{ error }}</p>

    <div class="form-actions">
      <button type="submit" class="button" :disabled="busy || values.rating === 0">
        {{ mode === "create" ? "Publish review" : "Save changes" }}
      </button>
      <button v-if="mode === 'edit'" type="button" class="button subtle" :disabled="busy" @click="emit('cancel')">
        Cancel
      </button>
      <span v-if="values.rating === 0" class="muted">Pick a star rating first</span>
    </div>
  </form>
</template>
