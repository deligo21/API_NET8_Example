using Microsoft.Data.SqlClient;
using PaymentAPI.Models;

namespace PaymentAPI.Services;

public interface IPaymentRepository
{
    Task<Guid> CreatePaymentAsync(CreatePaymentDto dto);
    Task<List<PaymentResponseDto>> GetPaymentsByCustomerIdAsync(Guid customerId);
    Task InitializeDatabaseAsync();
}

public class PaymentRepository : IPaymentRepository
{
    private readonly string _connectionString;
    private readonly ILogger<PaymentRepository> _logger;

    public PaymentRepository(IConfiguration configuration, ILogger<PaymentRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");
        _logger = logger;
    }

    public async Task InitializeDatabaseAsync()
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string createTableQuery = @"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Payments')
                BEGIN
                    CREATE TABLE Payments (
                        PaymentId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                        CustomerId UNIQUEIDENTIFIER NOT NULL,
                        ServiceProvider NVARCHAR(255) NOT NULL,
                        Amount DECIMAL(10, 2) NOT NULL,
                        Status NVARCHAR(50) NOT NULL DEFAULT 'pendiente',
                        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                    )
                    CREATE INDEX IX_CustomerId ON Payments(CustomerId)
                END";

            using var command = new SqlCommand(createTableQuery, connection);
            await command.ExecuteNonQueryAsync();
            _logger.LogInformation("Database initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing database");
            throw;
        }
    }

    public async Task<Guid> CreatePaymentAsync(CreatePaymentDto dto)
    {
        try
        {
            var paymentId = Guid.NewGuid();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string insertQuery = @"
                INSERT INTO Payments (PaymentId, CustomerId, ServiceProvider, Amount, Status, CreatedAt)
                VALUES (@PaymentId, @CustomerId, @ServiceProvider, @Amount, @Status, @CreatedAt)";

            using var command = new SqlCommand(insertQuery, connection);
            command.Parameters.AddWithValue("@PaymentId", paymentId);
            command.Parameters.AddWithValue("@CustomerId", dto.CustomerId);
            command.Parameters.AddWithValue("@ServiceProvider", dto.ServiceProvider);
            command.Parameters.AddWithValue("@Amount", dto.Amount);
            command.Parameters.AddWithValue("@Status", "pendiente");
            command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

            await command.ExecuteNonQueryAsync();
            _logger.LogInformation("Payment created: {PaymentId}", paymentId);
            return paymentId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment");
            throw;
        }
    }

    public async Task<List<PaymentResponseDto>> GetPaymentsByCustomerIdAsync(Guid customerId)
    {
        var payments = new List<PaymentResponseDto>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string selectQuery = @"
                SELECT PaymentId, CustomerId, ServiceProvider, Amount, Status, CreatedAt
                FROM Payments
                WHERE CustomerId = @CustomerId
                ORDER BY CreatedAt DESC";

            using var command = new SqlCommand(selectQuery, connection);
            command.Parameters.AddWithValue("@CustomerId", customerId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                payments.Add(new PaymentResponseDto
                {
                    PaymentId = reader.GetGuid(0),
                    CustomerId = reader.GetGuid(1),
                    ServiceProvider = reader.GetString(2),
                    Amount = reader.GetDecimal(3),
                    Status = reader.GetString(4),
                    CreatedAt = reader.GetDateTime(5)
                });
            }

            _logger.LogInformation("Retrieved {Count} payments for customer {CustomerId}", payments.Count, customerId);
            return payments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payments for customer {CustomerId}", customerId);
            throw;
        }
    }
}
