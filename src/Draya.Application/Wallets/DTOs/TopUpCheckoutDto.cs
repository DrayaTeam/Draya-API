namespace Draya.Application.Wallets.DTOs;

public record TopUpCheckoutDto(
    Guid TransactionId,
    decimal Amount,
    string CheckoutUrl
);
