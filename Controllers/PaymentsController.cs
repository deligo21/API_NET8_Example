using Microsoft.AspNetCore.Mvc;
using PaymentAPI.Exceptions;
using PaymentAPI.Models;
using PaymentAPI.Services;

namespace PaymentAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Registra un nuevo pago de servicio
    /// </summary>
    [HttpPost]
    [ProduceResponseType(StatusCodes.Status201Created)]
    [ProduceResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentResponseDto>> CreatePayment([FromBody] CreatePaymentDto dto)
    {
        try
        {
            var payment = await _paymentService.CreatePaymentAsync(dto);
            return CreatedAtAction(nameof(GetPaymentsByCustomerId), new { customerId = payment.CustomerId }, payment);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment");
            return StatusCode(500, new { error = "An error occurred while creating the payment" });
        }
    }

    /// <summary>
    /// Obtiene los pagos de un cliente específico
    /// </summary>
    [HttpGet]
    [ProduceResponseType(StatusCodes.Status200OK)]
    [ProduceResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<PaymentResponseDto>>> GetPaymentsByCustomerId([FromQuery] Guid customerId)
    {
        try
        {
            if (customerId == Guid.Empty)
                return BadRequest(new { error = "CustomerId is required" });

            var payments = await _paymentService.GetPaymentsByCustomerIdAsync(customerId);
            return Ok(payments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payments");
            return StatusCode(500, new { error = "An error occurred while retrieving payments" });
        }
    }
}
