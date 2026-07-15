using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductReviews.Api.Infrastructure;
using ProductReviews.Domain.Catalog;
using StrongTypes;

namespace ProductReviews.Api.Features.Catalog;

[ApiController]
[Route("api/products")]
public sealed class CatalogController(
    GetCatalogHandler getCatalog,
    GetProductDetailHandler getProductDetail) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<ProductSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductSummaryResponse>>> GetCatalog(CancellationToken cancellationToken)
    {
        var products = await getCatalog.HandleAsync(cancellationToken);
        return Ok(products.Select(ProductSummaryResponse.From).ToList());
    }

    [HttpGet("{slug}")]
    [AllowAnonymous]
    [ProducesResponseType<ProductDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailResponse>> GetProduct(NonEmptyString slug, CancellationToken cancellationToken)
    {
        var detail = await getProductDetail.HandleAsync(slug, User.AuthorIdOrNull(), cancellationToken);
        return detail is null ? NotFound() : Ok(ProductDetailResponse.From(detail));
    }
}
