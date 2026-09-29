namespace PaymentAPI.Models;

public class PaymentResponseDto
{
    public Guid PaymentId { get; set; }
    public Guid CustomerId { get; set; }
    public string ServiceProvider { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
