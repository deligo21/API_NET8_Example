namespace PaymentAPI.Models;

public class CreatePaymentDto
{
    public Guid CustomerId { get; set; }
    public string ServiceProvider { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
