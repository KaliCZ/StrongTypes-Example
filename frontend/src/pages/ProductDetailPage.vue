<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue";
import { api, type ProductDetail, type Review, type ReviewSortOption, type ReviewsPage } from "../api/client";
import { buildEditReviewBody, hasChanges, type ReviewFormValues } from "../api/editReviewBody";
import { problemMessage } from "../api/problem";
import { useAuth } from "../auth/useAuth";
import ReviewCard from "../components/ReviewCard.vue";
import ReviewForm from "../components/ReviewForm.vue";
import StarRating from "../components/StarRating.vue";

const props = defineProps<{ slug: string }>();

const auth = useAuth();

const product = ref<ProductDetail | null>(null);
const notFound = ref(false);
const reviewsPage = ref<ReviewsPage | null>(null);
const sort = ref<ReviewSortOption>("MostHelpful");
const starFilter = ref<Set<number>>(new Set());
const page = ref(1);
const pageSize = 10;

const busy = ref(false);
const formError = ref<string | null>(null);
const listError = ref<string | null>(null);
const editingReview = ref<Review | null>(null);

const totalPages = computed(() => {
  const totalCount = reviewsPage.value?.totalCount ?? 0;
  return Math.max(1, Math.ceil(totalCount / pageSize));
});

const canWriteReview = computed(() => auth.isSignedIn && product.value !== null && product.value.myReviewId == null);

async function loadProduct(): Promise<void> {
  const { data, error, response } = await api.GET("/api/products/{slug}", {
    params: { path: { slug: props.slug } },
  });
  if (error !== undefined || data === undefined) {
    notFound.value = response.status === 404;
    return;
  }
  product.value = data;
}

let reviewsRequestSequence = 0;

async function loadReviews(): Promise<void> {
  const requestNumber = ++reviewsRequestSequence;
  const { data, error } = await api.GET("/api/products/{slug}/reviews", {
    params: {
      path: { slug: props.slug },
      query: {
        sort: sort.value,
        ratings: [...starFilter.value],
        page: page.value,
        pageSize,
      },
    },
  });
  // A newer request superseded this one — dropping the stale response keeps
  // rapid sort/filter changes from applying out of order.
  if (requestNumber !== reviewsRequestSequence) {
    return;
  }
  if (error !== undefined || data === undefined) {
    listError.value = problemMessage(error);
    return;
  }
  listError.value = null;
  reviewsPage.value = data;
}

function toggleStarFilter(stars: number): void {
  const next = new Set(starFilter.value);
  if (!next.delete(stars)) {
    next.add(stars);
  }
  starFilter.value = next;
  page.value = 1;
}

async function submitReview(values: ReviewFormValues): Promise<void> {
  busy.value = true;
  formError.value = null;
  const { error } = await api.POST("/api/products/{slug}/reviews", {
    params: { path: { slug: props.slug } },
    body: {
      rating: values.rating,
      title: values.title,
      body: values.body,
      pros: values.pros === "" ? null : values.pros,
      cons: values.cons === "" ? null : values.cons,
    },
  });
  busy.value = false;
  if (error !== undefined) {
    formError.value = problemMessage(error);
    return;
  }
  await Promise.all([loadProduct(), loadReviews()]);
}

async function saveEdit(values: ReviewFormValues): Promise<void> {
  const original = editingReview.value;
  if (original === null) {
    return;
  }
  const body = buildEditReviewBody(original, values);
  if (!hasChanges(body)) {
    editingReview.value = null;
    return;
  }

  busy.value = true;
  formError.value = null;
  const { error } = await api.PATCH("/api/reviews/{id}", {
    params: { path: { id: original.id } },
    body,
  });
  busy.value = false;
  if (error !== undefined) {
    formError.value = problemMessage(error);
    return;
  }
  editingReview.value = null;
  await Promise.all([loadProduct(), loadReviews()]);
}

async function deleteReview(review: Review): Promise<void> {
  if (!window.confirm("Delete your review? This cannot be undone.")) {
    return;
  }
  busy.value = true;
  const { error } = await api.DELETE("/api/reviews/{id}", { params: { path: { id: review.id } } });
  busy.value = false;
  if (error !== undefined) {
    listError.value = problemMessage(error);
    return;
  }
  editingReview.value = null;
  await Promise.all([loadProduct(), loadReviews()]);
}

async function castVote(review: Review, isUpvote: boolean): Promise<void> {
  busy.value = true;
  const { data, error } = await api.PUT("/api/reviews/{id}/vote", {
    params: { path: { id: review.id } },
    body: { isUpvote },
  });
  busy.value = false;
  if (error !== undefined || data === undefined) {
    listError.value = problemMessage(error);
    return;
  }
  applyVoteResult(review, data.score, data.myVote ?? null);
}

async function removeVote(review: Review): Promise<void> {
  busy.value = true;
  const { data, error } = await api.DELETE("/api/reviews/{id}/vote", {
    params: { path: { id: review.id } },
  });
  busy.value = false;
  if (error !== undefined || data === undefined) {
    listError.value = problemMessage(error);
    return;
  }
  applyVoteResult(review, data.score, data.myVote ?? null);
}

function applyVoteResult(review: Review, score: number, myVote: boolean | null): void {
  review.score = score;
  review.myVote = myVote;
}

onMounted(async () => {
  await Promise.all([loadProduct(), loadReviews()]);
});

watch([sort, starFilter, page], () => void loadReviews());
// The viewer's own review context (mine/myVote flags) depends on who is signed in.
watch(
  () => auth.isSignedIn,
  () => void Promise.all([loadProduct(), loadReviews()]),
);
</script>

<template>
  <p v-if="notFound" class="error-banner">
    This product does not exist. <RouterLink to="/">Back to the catalog</RouterLink>
  </p>

  <template v-else-if="product">
    <section class="product-header">
      <img v-if="product.imageUrl" :src="product.imageUrl" :alt="product.name" />
      <div class="product-header-info">
        <h1>{{ product.name }}</h1>
        <p class="rating-line">
          <StarRating :value="product.averageRating ?? null" />
          <template v-if="product.averageRating != null">
            {{ product.averageRating.toFixed(1) }} · {{ product.reviewCount }} review{{
              product.reviewCount === 1 ? "" : "s"
            }}
          </template>
          <template v-else>Not yet rated</template>
        </p>
        <p>{{ product.description }}</p>
      </div>
    </section>

    <section>
      <h2>Reviews</h2>

      <ReviewForm v-if="canWriteReview" mode="create" :busy="busy" :error="formError" @submit="submitReview" />
      <p v-else-if="!auth.isSignedIn && auth.isAvailable" class="muted">
        <a href="#" @click.prevent="auth.signIn($route.fullPath)">Sign in</a> to write a review and vote.
      </p>

      <div class="review-controls">
        <label>
          Sort by
          <select v-model="sort">
            <option value="MostHelpful">Most helpful</option>
            <option value="Newest">Newest</option>
            <option value="HighestRating">Highest rating</option>
            <option value="LowestRating">Lowest rating</option>
          </select>
        </label>
        <span class="star-filter">
          Filter:
          <label v-for="stars in [5, 4, 3, 2, 1]" :key="stars">
            <input type="checkbox" :checked="starFilter.has(stars)" @change="toggleStarFilter(stars)" />
            {{ stars }}★
          </label>
        </span>
      </div>

      <p v-if="listError" class="error-banner">{{ listError }}</p>

      <template v-if="reviewsPage">
        <p v-if="reviewsPage.items.length === 0" class="empty-state">
          No reviews{{ starFilter.size > 0 ? " match the filter" : " yet" }}.
        </p>

        <template v-for="review in reviewsPage.items" :key="review.id">
          <ReviewForm
            v-if="editingReview?.id === review.id"
            mode="edit"
            :initial="review"
            :busy="busy"
            :error="formError"
            @submit="saveEdit"
            @cancel="editingReview = null"
          />
          <ReviewCard
            v-else
            :review="review"
            :can-vote="auth.isSignedIn"
            :busy="busy"
            @vote="(isUpvote) => castVote(review, isUpvote)"
            @remove-vote="removeVote(review)"
            @edit="editingReview = review"
            @remove="deleteReview(review)"
          />
        </template>

        <nav v-if="totalPages > 1" class="pagination">
          <button type="button" class="button subtle" :disabled="page <= 1" @click="page -= 1">Previous</button>
          <span class="muted">Page {{ page }} of {{ totalPages }}</span>
          <button type="button" class="button subtle" :disabled="page >= totalPages" @click="page += 1">Next</button>
        </nav>
      </template>
    </section>
  </template>

  <p v-else class="empty-state">Loading…</p>
</template>
