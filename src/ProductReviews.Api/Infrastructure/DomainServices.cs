using Microsoft.Extensions.DependencyInjection.Extensions;
using ProductReviews.Api.Features.Catalog;
using ProductReviews.Api.Features.Reviews;
using ProductReviews.Api.Features.Votes;

namespace ProductReviews.Api.Infrastructure;

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
