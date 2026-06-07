namespace Catalog.Products.Features.GetProductByName;

public record GetProductByNameResponse(IEnumerable<ProductDto> Products);

public class GetProductByNameEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/products/name/{name}", async (string name, ISender sender) =>
        {
            var result = await sender.Send(new GetProductByNameQuery(name));

            var response = result.Adapt<GetProductByNameResponse>();

            return Results.Ok(response);
        })
        .WithName("GetProductByName")
        .Produces<GetProductByNameResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Get Product By Name")
        .WithDescription("Get Product By Name");
    }
}
