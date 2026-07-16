using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductReviews.Api.Features.Catalog;

namespace ProductReviews.Api.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(product => product.Id).ValueGeneratedNever();
        builder.Property(product => product.Slug).HasMaxLength(100);
        builder.HasIndex(product => product.Slug).IsUnique();
        builder.Property(product => product.Name).HasMaxLength(200);
        builder.Property(product => product.Description).HasMaxLength(4000);
        builder.Property(product => product.ImageUrl).HasMaxLength(500);
    }
}
