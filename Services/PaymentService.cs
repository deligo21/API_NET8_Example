using PaymentAPI.Exceptions;
using PaymentAPI.Models;

namespace PaymentAPI.Services;

public interface IPaymentService
{
    Task<PaymentResponseDto> CreatePaymentAsync(CreatePaymentDto dto);
    Task<List<PaymentResponseDto>> GetPaymentsByCustomerIdAsync(Guid customerId);
}

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _repository;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IPaymentRepository repository, ILogger<PaymentService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<PaymentResponseDto> CreatePaymentAsync(CreatePaymentDto dto)
    {
        ValidatePayment(dto);

        var paymentId = await _repository.CreatePaymentAsync(dto);

        return new PaymentResponseDto
        {
            PaymentId = paymentId,
            CustomerId = dto.CustomerId,
            ServiceProvider = dto.ServiceProvider,
            Amount = dto.Amount,
            Status = "pendiente",
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<List<PaymentResponseDto>> GetPaymentsByCustomerIdAsync(Guid customerId)
    {
        return await _repository.GetPaymentsByCustomerIdAsync(customerId);
    }

    private void ValidatePayment(CreatePaymentDto dto)
    {
        if (dto.CustomerId == Guid.Empty)
            throw new ValidationException("CustomerId is required");

        if (string.IsNullOrWhiteSpace(dto.ServiceProvider))
            throw new ValidationException("ServiceProvider is required");

        if (dto.Amount <= 0)
            throw new ValidationException("Amount must be greater than zero");

        if (ContainsDollarSign(dto.Amount.ToString()))
            throw new ValidationException("Currency must be in Bolivianos (Bs), not dollars");

        if (dto.Amount > 1500)
            throw new ValidationException("Amount cannot exceed 1500 Bs");
    }

    private bool ContainsDollarSign(string amountStr)
    {
        return amountStr.Contains("$");
    }
}
