using Microsoft.EntityFrameworkCore;
using ProductReviews.Domain.Persistence;

namespace ProductReviews.Domain.Catalog;

public sealed class GetCatalogHandler(ReviewsDbContext dbContext)
{
    public async Task<IReadOnlyList<Product>> HandleAsync(CancellationToken cancellationToken)
        => await dbContext.Products
            .AsNoTracking()
            .OrderBy(product => product.Id)
            .ToListAsync(cancellationToken);
}
