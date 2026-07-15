using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProductReviews.Domain.Catalog;
using ProductReviews.Domain.Reviews;
using ProductReviews.Domain.Votes;

namespace ProductReviews.Domain;

public static class DomainServices
{
    public static IServiceCollection AddReviewsDomain(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<GetCatalogHandler>();
        services.AddScoped<GetProductDetailHandler>();
        services.AddScoped<GetReviewsPageHandler>();
        services.AddScoped<SubmitReviewHandler>();
        services.AddScoped<EditReviewHandler>();
        services.AddScoped<DeleteReviewHandler>();
        services.AddScoped<CastVoteHandler>();
        services.AddScoped<RemoveVoteHandler>();

        return services;
    }
}
