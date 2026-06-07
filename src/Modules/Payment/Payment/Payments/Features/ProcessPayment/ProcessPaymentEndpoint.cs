using Payment.Payments.Dtos;

namespace Payment.Payments.Features.ProcessPayment;

public record ProcessPaymentRequest(PaymentDto Payment);
public record ProcessPaymentResponse(Guid PaymentId, bool IsSuccess);

public class ProcessPaymentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/payments", async (ProcessPaymentRequest request, ISender sender) =>
        {
            var command = request.Adapt<ProcessPaymentCommand>();

            var result = await sender.Send(command);

            var response = result.Adapt<ProcessPaymentResponse>();

            return Results.Ok(response);
        })
        .WithName("ProcessPayment")
        .Produces<ProcessPaymentResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Process Payment")
        .WithDescription("Process Payment");
    }
}
