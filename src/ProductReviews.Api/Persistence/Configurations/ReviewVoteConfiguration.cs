using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductReviews.Api.Features.Reviews;
using ProductReviews.Api.Features.Votes;

namespace ProductReviews.Api.Persistence.Configurations;

internal sealed class ReviewVoteConfiguration : IEntityTypeConfiguration<ReviewVote>
{
    public void Configure(EntityTypeBuilder<ReviewVote> builder)
    {
        builder.HasKey(vote => new { vote.ReviewId, vote.VoterId });

        builder.HasOne<Review>()
            .WithMany(review => review.Votes)
            .HasForeignKey(vote => vote.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
