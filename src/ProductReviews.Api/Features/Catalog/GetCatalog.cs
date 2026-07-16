using Microsoft.EntityFrameworkCore;
using ProductReviews.Api.Persistence;

namespace ProductReviews.Api.Features.Catalog;

public sealed class GetCatalogHandler(ReviewsDbContext dbContext)
{
    public async Task<IReadOnlyList<Product>> HandleAsync(CancellationToken cancellationToken)
        => await dbContext.Products
            .AsNoTracking()
            .OrderBy(product => product.Id)
            .ToListAsync(cancellationToken);
}
