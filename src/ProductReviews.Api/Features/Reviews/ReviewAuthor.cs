using StrongTypes;

namespace ProductReviews.Api.Features.Reviews;

/// <summary>The acting reviewer, as the domain sees them: an opaque stable id
/// (derived from the identity provider's subject, see ADR-0003) and the display
/// name snapshotted onto anything they write.</summary>
public sealed record ReviewAuthor(Guid AuthorId, NonEmptyString DisplayName);
