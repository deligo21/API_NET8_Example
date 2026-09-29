using Microsoft.Data.SqlClient;
using PaymentAPI.Models;

namespace PaymentAPI.Services;

/// <summary>
/// EJEMPLOS de métodos que se conectan directamente a Stored Procedures
/// Este archivo muestra diferentes patrones para llamar a SP en SQL Server
/// </summary>
public class PaymentRepositoryStoredProcExamples
{
    private readonly string _connectionString;
    private readonly ILogger<PaymentRepositoryStoredProcExamples> _logger;

    public PaymentRepositoryStoredProcExamples(IConfiguration configuration, ILogger<PaymentRepositoryStoredProcExamples> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");
        _logger = logger;
    }

    // ============================================================================
    // EJEMPLO 1: Crear un pago usando un Stored Procedure
    // SP: sp_CreatePayment
    // ============================================================================
    public async Task<Guid> CreatePaymentWithStoredProcAsync(CreatePaymentDto dto)
    {
        try
        {
            var paymentId = Guid.NewGuid();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // IMPORTANTE: CommandType.StoredProcedure
            using var command = new SqlCommand("sp_CreatePayment", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            // Agregar parámetros
            command.Parameters.AddWithValue("@PaymentId", paymentId);
            command.Parameters.AddWithValue("@CustomerId", dto.CustomerId);
            command.Parameters.AddWithValue("@ServiceProvider", dto.ServiceProvider);
            command.Parameters.AddWithValue("@Amount", dto.Amount);
            command.Parameters.AddWithValue("@Status", "pendiente");
            command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

            await command.ExecuteNonQueryAsync();
            _logger.LogInformation("Payment created via SP: {PaymentId}", paymentId);
            return paymentId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment with stored procedure");
            throw;
        }
    }

    // ============================================================================
    // EJEMPLO 2: Obtener pagos con parámetros de salida (OUTPUT)
    // SP: sp_GetPaymentsByCustomer
    // ============================================================================
    public async Task<(List<PaymentResponseDto> payments, int totalCount)>
        GetPaymentsByCustomerWithCountAsync(Guid customerId)
    {
        var payments = new List<PaymentResponseDto>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_GetPaymentsByCustomer", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            // Parámetro de entrada
            command.Parameters.AddWithValue("@CustomerId", customerId);

            // Parámetro de salida (OUTPUT)
            var totalCountParam = new SqlParameter("@TotalCount", System.Data.SqlDbType.Int)
            {
                Direction = System.Data.ParameterDirection.Output
            };
            command.Parameters.Add(totalCountParam);

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

            int totalCount = (int?)totalCountParam.Value ?? 0;
            _logger.LogInformation("Retrieved {Count} payments (Total: {Total}) for customer {CustomerId}",
                payments.Count, totalCount, customerId);

            return (payments, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payments with stored procedure");
            throw;
        }
    }

    // ============================================================================
    // EJEMPLO 3: Actualizar estado de un pago
    // SP: sp_UpdatePaymentStatus
    // ============================================================================
    public async Task<bool> UpdatePaymentStatusAsync(Guid paymentId, string newStatus)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_UpdatePaymentStatus", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@PaymentId", paymentId);
            command.Parameters.AddWithValue("@NewStatus", newStatus);
            command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

            int rowsAffected = await command.ExecuteNonQueryAsync();

            _logger.LogInformation("Payment {PaymentId} status updated to {Status}", paymentId, newStatus);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment status");
            throw;
        }
    }

    // ============================================================================
    // EJEMPLO 4: Obtener resumen de pagos con múltiples resultados
    // SP: sp_GetPaymentSummary
    // Devuelve: pagos + estadísticas en diferentes result sets
    // ============================================================================
    public async Task<(List<PaymentResponseDto> payments, PaymentStatistics stats)>
        GetPaymentSummaryAsync(Guid customerId)
    {
        var payments = new List<PaymentResponseDto>();
        var stats = new PaymentStatistics();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_GetPaymentSummary", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@CustomerId", customerId);

            using var reader = await command.ExecuteReaderAsync();

            // PRIMER result set: los pagos
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

            // SEGUNDO result set: estadísticas
            if (await reader.NextResultAsync())
            {
                if (await reader.ReadAsync())
                {
                    stats.TotalAmount = reader.GetDecimal(0);
                    stats.PaymentCount = reader.GetInt32(1);
                    stats.AverageAmount = reader.GetDecimal(2);
                    stats.MaxAmount = reader.GetDecimal(3);
                }
            }

            _logger.LogInformation("Retrieved summary for customer {CustomerId}: {Count} payments, Total: {Total}",
                customerId, payments.Count, stats.TotalAmount);

            return (payments, stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment summary");
            throw;
        }
    }

    // ============================================================================
    // EJEMPLO 5: Procesar un pago y devolver un valor de retorno del SP
    // SP: sp_ProcessPayment (devuelve un código de resultado)
    // ============================================================================
    public async Task<int> ProcessPaymentAsync(Guid paymentId, string processingMethod)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_ProcessPayment", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@PaymentId", paymentId);
            command.Parameters.AddWithValue("@ProcessingMethod", processingMethod);
            command.Parameters.AddWithValue("@ProcessedAt", DateTime.UtcNow);

            // El valor de retorno del SP (RETURN)
            var returnValue = new SqlParameter
            {
                ParameterName = "@ReturnValue",
                Direction = System.Data.ParameterDirection.ReturnValue
            };
            command.Parameters.Add(returnValue);

            await command.ExecuteNonQueryAsync();

            int result = (int)returnValue.Value;
            _logger.LogInformation("Payment {PaymentId} processed with result code: {ResultCode}", paymentId, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment");
            throw;
        }
    }

    // ============================================================================
    // EJEMPLO 6: Llamar a un SP que devuelve datos escalares (un solo valor)
    // SP: sp_GetTotalPaymentsByCustomer
    // ============================================================================
    public async Task<decimal> GetTotalPaymentsByCustomerAsync(Guid customerId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_GetTotalPaymentsByCustomer", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@CustomerId", customerId);

            var result = await command.ExecuteScalarAsync();
            decimal total = result != null ? Convert.ToDecimal(result) : 0m;

            _logger.LogInformation("Total payments for customer {CustomerId}: {Total}", customerId, total);
            return total;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total payments");
            throw;
        }
    }

    // ============================================================================
    // EJEMPLO 7: Stored Procedure con paginación
    // SP: sp_GetPaymentsPaged
    // ============================================================================
    public async Task<(List<PaymentResponseDto> payments, int totalRecords)>
        GetPaymentsPagedAsync(Guid customerId, int pageNumber, int pageSize)
    {
        var payments = new List<PaymentResponseDto>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_GetPaymentsPaged", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@CustomerId", customerId);
            command.Parameters.AddWithValue("@PageNumber", pageNumber);
            command.Parameters.AddWithValue("@PageSize", pageSize);

            var totalRecordsParam = new SqlParameter("@TotalRecords", System.Data.SqlDbType.Int)
            {
                Direction = System.Data.ParameterDirection.Output
            };
            command.Parameters.Add(totalRecordsParam);

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

            int totalRecords = (int?)totalRecordsParam.Value ?? 0;
            _logger.LogInformation("Retrieved page {Page} with {Count} payments (Total: {Total})",
                pageNumber, payments.Count, totalRecords);

            return (payments, totalRecords);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paged payments");
            throw;
        }
    }

    // ============================================================================
    // EJEMPLO 8: SP que devuelve datos XML
    // SP: sp_ExportPaymentsSummary
    // ============================================================================
    public async Task<string> ExportPaymentsSummaryAsXmlAsync(Guid customerId, DateTime startDate, DateTime endDate)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_ExportPaymentsSummary", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@CustomerId", customerId);
            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);

            var result = await command.ExecuteScalarAsync();
            string xmlData = result?.ToString() ?? string.Empty;

            _logger.LogInformation("Exported payments summary for customer {CustomerId} as XML", customerId);
            return xmlData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting payments summary");
            throw;
        }
    }

    // ============================================================================
    // EJEMPLO 9: Transacción con múltiples llamadas a SP
    // ============================================================================
    public async Task<bool> TransferPaymentAsync(Guid sourcePaymentId, Guid targetCustomerId)
    {
        SqlTransaction transaction = null;
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Iniciar transacción
            transaction = connection.BeginTransaction();

            // Primer SP: obtener el pago a transferir
            using var getCommand = new SqlCommand("sp_GetPaymentById", connection, transaction)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            getCommand.Parameters.AddWithValue("@PaymentId", sourcePaymentId);

            // Segundo SP: eliminar el pago original
            using var deleteCommand = new SqlCommand("sp_DeletePayment", connection, transaction)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            deleteCommand.Parameters.AddWithValue("@PaymentId", sourcePaymentId);
            await deleteCommand.ExecuteNonQueryAsync();

            // Tercer SP: crear nuevo pago con el mismo monto
            using var createCommand = new SqlCommand("sp_CreatePaymentForCustomer", connection, transaction)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            createCommand.Parameters.AddWithValue("@TargetCustomerId", targetCustomerId);
            createCommand.Parameters.AddWithValue("@SourcePaymentId", sourcePaymentId);
            await createCommand.ExecuteNonQueryAsync();

            // Confirmar transacción
            await transaction.CommitAsync();
            _logger.LogInformation("Payment transferred from {SourceId} to customer {TargetId}",
                sourcePaymentId, targetCustomerId);
            return true;
        }
        catch (Exception ex)
        {
            // Revertir en caso de error
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }
            _logger.LogError(ex, "Error transferring payment");
            throw;
        }
    }

    // ============================================================================
    // EJEMPLO 10: Llamar a SP con parámetros con valores NULL
    // ============================================================================
    public async Task<List<PaymentResponseDto>> GetPaymentsWithFiltersAsync(
        Guid? customerId = null,
        string? serviceProvider = null,
        decimal? minAmount = null,
        decimal? maxAmount = null)
    {
        var payments = new List<PaymentResponseDto>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_GetPaymentsFiltered", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            // Parámetros opcionales con NULL si no se proporcionan
            command.Parameters.AddWithValue("@CustomerId", customerId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ServiceProvider", serviceProvider ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@MinAmount", minAmount ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@MaxAmount", maxAmount ?? (object)DBNull.Value);

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

            _logger.LogInformation("Retrieved {Count} payments with filters", payments.Count);
            return payments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving filtered payments");
            throw;
        }
    }
}

// ============================================================================
// CLASE DE APOYO: Estadísticas de Pagos
// ============================================================================
public class PaymentStatistics
{
    public decimal TotalAmount { get; set; }
    public int PaymentCount { get; set; }
    public decimal AverageAmount { get; set; }
    public decimal MaxAmount { get; set; }
}
