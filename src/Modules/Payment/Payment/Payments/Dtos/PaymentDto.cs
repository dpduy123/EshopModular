namespace Payment.Payments.Dtos;

public record PaymentDto(
    Guid OrderId,
    decimal Amount,
    string CardName,
    string CardNumber,
    string Expiration,
    string Cvv,
    int PaymentMethod
);
