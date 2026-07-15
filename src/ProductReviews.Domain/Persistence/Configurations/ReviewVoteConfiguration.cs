using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductReviews.Domain.Reviews;
using ProductReviews.Domain.Votes;

namespace ProductReviews.Domain.Persistence.Configurations;

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
