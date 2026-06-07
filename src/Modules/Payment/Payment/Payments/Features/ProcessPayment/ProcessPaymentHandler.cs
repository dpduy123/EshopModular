using FluentValidation;
using Payment.Payments.Dtos;

namespace Payment.Payments.Features.ProcessPayment;

public record ProcessPaymentCommand(PaymentDto Payment)
    : ICommand<ProcessPaymentResult>;

public record ProcessPaymentResult(Guid PaymentId, bool IsSuccess);

public class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
{
    public ProcessPaymentCommandValidator()
    {
        RuleFor(x => x.Payment.OrderId).NotEmpty().WithMessage("OrderId is required");
        RuleFor(x => x.Payment.Amount).GreaterThan(0).WithMessage("Amount must be greater than 0");
        RuleFor(x => x.Payment.CardNumber).NotEmpty().WithMessage("CardNumber is required");
        RuleFor(x => x.Payment.Expiration).NotEmpty().WithMessage("Expiration is required");
        RuleFor(x => x.Payment.Cvv).NotEmpty().WithMessage("CVV is required");
    }
}

internal class ProcessPaymentHandler : ICommandHandler<ProcessPaymentCommand, ProcessPaymentResult>
{
    public Task<ProcessPaymentResult> Handle(ProcessPaymentCommand command, CancellationToken cancellationToken)
    {
        // Giả lập xử lý thanh toán thành công
        var paymentId = Guid.NewGuid();
        return Task.FromResult(new ProcessPaymentResult(paymentId, true));
    }
}
