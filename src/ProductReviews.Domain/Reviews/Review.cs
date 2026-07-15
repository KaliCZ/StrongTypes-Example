using ProductReviews.Domain.Votes;
using StrongTypes;

namespace ProductReviews.Domain.Reviews;

public sealed class Review
{
    private Review()
    {
        AuthorName = null!;
        Title = null!;
        Body = null!;
    }

    // authorDisplayName deliberately matches no property name, so EF's
    // constructor binding can never pick this ctor and re-run the id generation.
    public Review(
        long productId,
        Guid authorId,
        NonEmptyString authorDisplayName,
        Rating rating,
        NonEmptyString title,
        NonEmptyString body,
        NonEmptyString? pros,
        NonEmptyString? cons,
        DateTime createdAtUtc)
    {
        Id = Guid.CreateVersion7(new DateTimeOffset(createdAtUtc, TimeSpan.Zero));
        ProductId = productId;
        AuthorId = authorId;
        AuthorName = authorDisplayName;
        Rating = rating;
        Title = title;
        Body = body;
        Pros = pros;
        Cons = cons;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>UUIDv7, so the id doubles as a created-at tiebreaker for sorting.</summary>
    public Guid Id { get; private set; }

    public long ProductId { get; private set; }

    public Guid AuthorId { get; private set; }

    public NonEmptyString AuthorName { get; private set; }

    public Rating Rating { get; private set; }

    public NonEmptyString Title { get; private set; }

    public NonEmptyString Body { get; private set; }

    public NonEmptyString? Pros { get; private set; }

    public NonEmptyString? Cons { get; private set; }

    /// <summary>Denormalized helpfulness: upvotes minus downvotes, always recomputed from the vote rows.</summary>
    public int Score { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Null until the first edit.</summary>
    public DateTime? UpdatedAtUtc { get; private set; }

    public ICollection<ReviewVote> Votes { get; } = [];

    /// <summary>PATCH semantics: a null parameter leaves the field unchanged; for the
    /// clearable optionals, <c>Maybe.None</c> clears and <c>Maybe.Some</c> replaces.</summary>
    public void ApplyEdit(
        Rating? rating,
        NonEmptyString? title,
        NonEmptyString? body,
        Maybe<NonEmptyString>? pros,
        Maybe<NonEmptyString>? cons,
        DateTime editedAtUtc)
    {
        if (rating is { } newRating)
        {
            Rating = newRating;
        }
        if (title is { } newTitle)
        {
            Title = newTitle;
        }
        if (body is { } newBody)
        {
            Body = newBody;
        }
        if (pros is { } prosChange)
        {
            Pros = prosChange.Value;
        }
        if (cons is { } consChange)
        {
            Cons = consChange.Value;
        }
        UpdatedAtUtc = editedAtUtc;
    }

    public void RefreshScore(IReadOnlyCollection<ReviewVote> currentVotes)
        => Score = currentVotes.Sum(vote => vote.ScoreContribution);
}
