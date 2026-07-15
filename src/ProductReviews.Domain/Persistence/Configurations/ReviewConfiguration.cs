using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductReviews.Domain.Catalog;
using ProductReviews.Domain.Reviews;

namespace ProductReviews.Domain.Persistence.Configurations;

internal sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.Property(review => review.Id).ValueGeneratedNever();
        builder.Property(review => review.AuthorName).HasMaxLength(100);
        builder.Property(review => review.Title).HasMaxLength(200);
        builder.Property(review => review.Body).HasMaxLength(4000);
        builder.Property(review => review.Pros).HasMaxLength(500);
        builder.Property(review => review.Cons).HasMaxLength(500);

        builder.HasOne<Product>()
            .WithMany(product => product.Reviews)
            .HasForeignKey(review => review.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // One review per reviewer per product; deleting the review frees the slot.
        builder.HasIndex(review => new { review.ProductId, review.AuthorId })
            .IsUnique()
            .HasDatabaseName("uq_reviews_product_author");

        // Covering indexes for the "most helpful" and rating sorts; "newest" rides the UUIDv7 PK.
        builder.HasIndex(review => new { review.ProductId, review.Score, review.Id })
            .HasDatabaseName("ix_reviews_helpful");
        builder.HasIndex(review => new { review.ProductId, review.Rating, review.Score, review.Id })
            .HasDatabaseName("ix_reviews_rating");

        // Backstop for the Rating invariant — the type makes invalid values unrepresentable
        // in the application; this guards direct SQL writes.
        builder.ToTable(table => table.HasCheckConstraint("ck_reviews_rating", "\"Rating\" BETWEEN 1 AND 5"));
    }
}
