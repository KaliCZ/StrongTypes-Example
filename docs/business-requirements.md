# Product Reviews — Business Requirements

> What the application must do, in product terms. No technology here — see
> [technical-requirements.md](technical-requirements.md) for how it is built.

## Purpose

A product-reviews platform: visitors browse a catalog of products and read
reviews; signed-in reviewers write reviews and vote on how helpful other
people's reviews are.

## Users

- **Visitor** — anyone, no account needed. Browses the catalog and reads
  everything, but cannot write or vote.
- **Reviewer** — a signed-in user. Everything a visitor can do, plus writing,
  editing, and deleting their own reviews and voting on other people's.

## Requirements

**Catalog**

- The catalog lists every product with its name, photo (when one exists),
  average rating, and how many reviews it has.
- Products come from an upstream catalog. This application never creates,
  edits, or removes products.
- Each product has a stable, human-readable web address (a slug), so product
  pages can be linked and shared.

**Product detail**

- A product page shows the product's name, photo, description, average rating,
  review count, and its reviews.
- Reviews on the page can be sorted by **most helpful** (the default),
  **newest**, or **rating**, and narrowed to one or more star values
  (e.g. only 1★ and 2★ reviews).
- Long review lists are paged; the reader is never handed an unbounded list.

**Writing a review**

- Only signed-in reviewers can write reviews.
- A review consists of: a whole-star rating from 1 to 5, a title, and a body
  text — plus optional **pros** and **cons** lines.
- A reviewer can have **at most one review per product**. The product page
  makes it clear when the viewer has already reviewed the product and shows
  which review is theirs.
- A submitted review is publicly visible immediately — there is no moderation
  step.
- A review displays its author's display name and when it was written.

**Editing a review**

- A reviewer can edit only their own review.
- An edit may change any part independently: the rating, the title, the body,
  and the pros/cons lines. Pros and cons can also be **removed** by an edit,
  not just changed — "leave it as it is", "change it", and "clear it" are
  three distinct choices.
- An edited review shows that (and when) it was last updated.

**Deleting a review**

- A reviewer can delete only their own review.
- Deleting is permanent: the review and all votes cast on it are gone.
- After deleting, the reviewer may write a fresh review for that product.

**Voting on helpfulness**

- A signed-in reviewer can mark any review as **helpful** or **not helpful** —
  except their own review, which they can never vote on.
- One vote per reviewer per review. A reviewer can switch their vote (helpful
  ⇄ not helpful) or withdraw it entirely at any time.
- A review's **helpfulness score** is the number of helpful votes minus the
  number of not-helpful votes. The score is shown next to the review, and the
  viewer can always see which way they themselves voted.

**Rating aggregation**

- A product's **average rating** is the plain mean of the star ratings of its
  current reviews, shown together with the review count.
- A product with no reviews presents as "not yet rated" — it is never shown as
  rated zero.
- Deleting or editing a review is reflected in the average immediately.

**Access**

- Browsing and reading never require an account.
- Writing, editing, deleting, and voting always require being signed in.
- Reviewers only ever act as themselves: no user can edit, delete, or vote on
  behalf of another.

## Non-goals

- Product management (creating or editing products) — the catalog is fixed
  upstream data.
- Review moderation, flagging, or admin tooling.
- Photos or attachments on reviews.
- Replies, comments, or threads under reviews.
- User profiles, avatars, or a "my reviews" overview page.
- Catalog search or product categories.
- Notifications of any kind.
