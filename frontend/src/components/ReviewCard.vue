<script setup lang="ts">
import type { Review } from "../api/client";
import { formatDate } from "../formatDate";
import StarRating from "./StarRating.vue";

const props = defineProps<{
  review: Review;
  /** Whether the viewer may vote at all (signed in). Own reviews are excluded regardless. */
  canVote: boolean;
  busy?: boolean;
}>();

const emit = defineEmits<{
  vote: [isUpvote: boolean];
  removeVote: [];
  edit: [];
  remove: [];
}>();

function toggleVote(isUpvote: boolean): void {
  if (props.review.myVote === isUpvote) {
    emit("removeVote");
  } else {
    emit("vote", isUpvote);
  }
}
</script>

<template>
  <article class="review-card" :class="{ mine: review.mine }">
    <header class="review-card-header">
      <StarRating :value="review.rating" />
      <h3>{{ review.title }}</h3>
      <span v-if="review.mine" class="badge">Your review</span>
    </header>

    <p class="review-meta">
      {{ review.authorName }} · {{ formatDate(review.createdAtUtc) }}
      <template v-if="review.updatedAtUtc"> · edited {{ formatDate(review.updatedAtUtc) }}</template>
    </p>

    <p class="review-body">{{ review.body }}</p>
    <p v-if="review.pros" class="review-pro">＋ {{ review.pros }}</p>
    <p v-if="review.cons" class="review-con">－ {{ review.cons }}</p>

    <footer class="review-card-footer">
      <div class="vote-controls">
        <span class="score" :class="{ positive: review.score > 0, negative: review.score < 0 }">
          {{ review.score > 0 ? `+${review.score}` : review.score }}
        </span>
        <template v-if="review.mine">
          <span class="muted">You can't vote on your own review</span>
        </template>
        <template v-else>
          <button
            type="button"
            class="vote-button"
            :class="{ active: review.myVote === true }"
            :disabled="!canVote || busy"
            :title="canVote ? undefined : 'Sign in to vote'"
            @click="toggleVote(true)"
          >
            👍 Helpful
          </button>
          <button
            type="button"
            class="vote-button"
            :class="{ active: review.myVote === false }"
            :disabled="!canVote || busy"
            :title="canVote ? undefined : 'Sign in to vote'"
            @click="toggleVote(false)"
          >
            👎 Not helpful
          </button>
        </template>
      </div>

      <div v-if="review.mine" class="own-actions">
        <button type="button" class="button subtle" :disabled="busy" @click="emit('edit')">Edit</button>
        <button type="button" class="button danger" :disabled="busy" @click="emit('remove')">Delete</button>
      </div>
    </footer>
  </article>
</template>
