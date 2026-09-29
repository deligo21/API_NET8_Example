# Guía de Stored Procedures en .NET - Payment API

Este documento explica cómo conectarse directamente a **Stored Procedures** de SQL Server desde C#/.NET usando **ADO.NET**.

---

## 📋 Conceptos Básicos

### ¿Qué es un Stored Procedure?
Un **Stored Procedure** es un conjunto de comandos SQL precompilados almacenados en la base de datos. Ventajas:

- ✅ **Mejor rendimiento**: SQL precompilado en el servidor
- ✅ **Seguridad**: Evita SQL injection si se usan parámetros correctamente
- ✅ **Reutilización**: Lógica centralizada en la BD
- ✅ **Transacciones**: Manejo robusto de múltiples operaciones

---

## 🔧 Pasos Básicos para Llamar un SP en C#

```csharp
// 1. Crear conexión
using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();

// 2. Crear comando
using var command = new SqlCommand("sp_NombreProcedimiento", connection)
{
    CommandType = System.Data.CommandType.StoredProcedure  // IMPORTANTE
};

// 3. Agregar parámetros
command.Parameters.AddWithValue("@Parametro1", valor1);
command.Parameters.AddWithValue("@Parametro2", valor2);

// 4. Ejecutar
await command.ExecuteNonQueryAsync();  // Para INSERT, UPDATE, DELETE
// o
var result = await command.ExecuteScalarAsync();  // Para un solo valor
// o
using var reader = await command.ExecuteReaderAsync();  // Para múltiples filas
```

---

## 📚 10 Ejemplos Prácticos

### EJEMPLO 1: Crear un Pago (Sin Retorno)

**Uso**: Insertar un nuevo pago en la BD.

```csharp
// C# Code
public async Task<Guid> CreatePaymentWithStoredProcAsync(CreatePaymentDto dto)
{
    var paymentId = Guid.NewGuid();
    
    using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();

    using var command = new SqlCommand("sp_CreatePayment", connection)
    {
        CommandType = System.Data.CommandType.StoredProcedure
    };

    command.Parameters.AddWithValue("@PaymentId", paymentId);
    command.Parameters.AddWithValue("@CustomerId", dto.CustomerId);
    command.Parameters.AddWithValue("@ServiceProvider", dto.ServiceProvider);
    command.Parameters.AddWithValue("@Amount", dto.Amount);
    command.Parameters.AddWithValue("@Status", "pendiente");
    command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

    await command.ExecuteNonQueryAsync();
    return paymentId;
}
```

**SQL Procedure**:
```sql
CREATE PROCEDURE sp_CreatePayment
    @PaymentId UNIQUEIDENTIFIER,
    @CustomerId UNIQUEIDENTIFIER,
    @ServiceProvider NVARCHAR(255),
    @Amount DECIMAL(10, 2),
    @Status NVARCHAR(50),
    @CreatedAt DATETIME2
AS
BEGIN
    INSERT INTO Payments (PaymentId, CustomerId, ServiceProvider, Amount, Status, CreatedAt)
    VALUES (@PaymentId, @CustomerId, @ServiceProvider, @Amount, @Status, @CreatedAt);
END
```

---

### EJEMPLO 2: Parámetros de Salida (OUTPUT)

**Uso**: Obtener datos Y un contador al mismo tiempo.

```csharp
public async Task<(List<PaymentResponseDto> payments, int totalCount)> 
    GetPaymentsByCustomerWithCountAsync(Guid customerId)
{
    var payments = new List<PaymentResponseDto>();

    using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();

    using var command = new SqlCommand("sp_GetPaymentsByCustomer", connection)
    {
        CommandType = System.Data.CommandType.StoredProcedure
    };

    command.Parameters.AddWithValue("@CustomerId", customerId);

    // IMPORTANTE: Parámetro OUTPUT
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

    // Obtener el valor OUTPUT
    int totalCount = (int?)totalCountParam.Value ?? 0;
    return (payments, totalCount);
}
```

**SQL Procedure**:
```sql
CREATE PROCEDURE sp_GetPaymentsByCustomer
    @CustomerId UNIQUEIDENTIFIER,
    @TotalCount INT OUTPUT
AS
BEGIN
    SELECT @TotalCount = COUNT(*)
    FROM Payments
    WHERE CustomerId = @CustomerId;
    
    SELECT PaymentId, CustomerId, ServiceProvider, Amount, Status, CreatedAt
    FROM Payments
    WHERE CustomerId = @CustomerId
    ORDER BY CreatedAt DESC;
END
```

---

### EJEMPLO 3: Ejecutar Escalar (Un Solo Valor)

**Uso**: Obtener un solo valor (suma, promedio, máximo, etc.).

```csharp
public async Task<decimal> GetTotalPaymentsByCustomerAsync(Guid customerId)
{
    using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();

    using var command = new SqlCommand("sp_GetTotalPaymentsByCustomer", connection)
    {
        CommandType = System.Data.CommandType.StoredProcedure
    };

    command.Parameters.AddWithValue("@CustomerId", customerId);

    // ExecuteScalarAsync devuelve un solo valor
    var result = await command.ExecuteScalarAsync();
    return result != null ? Convert.ToDecimal(result) : 0m;
}
```

**SQL Procedure**:
```sql
CREATE PROCEDURE sp_GetTotalPaymentsByCustomer
    @CustomerId UNIQUEIDENTIFIER
AS
BEGIN
    SELECT SUM(Amount)
    FROM Payments
    WHERE CustomerId = @CustomerId;
END
```

---

### EJEMPLO 4: Múltiples Result Sets

**Uso**: Devolver diferentes tablas de datos en una sola llamada.

```csharp
public async Task<(List<PaymentResponseDto> payments, PaymentStatistics stats)> 
    GetPaymentSummaryAsync(Guid customerId)
{
    var payments = new List<PaymentResponseDto>();
    var stats = new PaymentStatistics();

    using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();

    using var command = new SqlCommand("sp_GetPaymentSummary", connection)
    {
        CommandType = System.Data.CommandType.StoredProcedure
    };

    command.Parameters.AddWithValue("@CustomerId", customerId);

    using var reader = await command.ExecuteReaderAsync();
    
    // PRIMER RESULT SET
    while (await reader.ReadAsync())
    {
        payments.Add(new PaymentResponseDto { /* ... */ });
    }

    // Ir al SIGUIENTE RESULT SET
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

    return (payments, stats);
}
```

**SQL Procedure**:
```sql
CREATE PROCEDURE sp_GetPaymentSummary
    @CustomerId UNIQUEIDENTIFIER
AS
BEGIN
    -- PRIMER RESULT SET: Pagos
    SELECT PaymentId, CustomerId, ServiceProvider, Amount, Status, CreatedAt
    FROM Payments
    WHERE CustomerId = @CustomerId
    ORDER BY CreatedAt DESC;
    
    -- SEGUNDO RESULT SET: Estadísticas
    SELECT 
        SUM(Amount) AS TotalAmount,
        COUNT(*) AS PaymentCount,
        AVG(Amount) AS AverageAmount,
        MAX(Amount) AS MaxAmount
    FROM Payments
    WHERE CustomerId = @CustomerId;
END
```

---

### EJEMPLO 5: Return Value (Código de Retorno)

**Uso**: El SP devuelve un código numérico indicando éxito/error.

```csharp
public async Task<int> ProcessPaymentAsync(Guid paymentId, string processingMethod)
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

    // Capturar el RETURN VALUE
    var returnValue = new SqlParameter
    {
        ParameterName = "@ReturnValue",
        Direction = System.Data.ParameterDirection.ReturnValue
    };
    command.Parameters.Add(returnValue);

    await command.ExecuteNonQueryAsync();

    int result = (int)returnValue.Value;
    // 0 = Éxito, 1 = No encontrado, 2 = Límite excedido, -1 = Error
    return result;
}
```

**SQL Procedure**:
```sql
CREATE PROCEDURE sp_ProcessPayment
    @PaymentId UNIQUEIDENTIFIER,
    @ProcessingMethod NVARCHAR(50),
    @ProcessedAt DATETIME2
AS
BEGIN
    IF NOT EXISTS(SELECT 1 FROM Payments WHERE PaymentId = @PaymentId)
        RETURN 1;  -- No encontrado
    
    IF (SELECT Amount FROM Payments WHERE PaymentId = @PaymentId) > 1500
        RETURN 2;  -- Límite excedido
    
    UPDATE Payments SET Status = 'procesado' WHERE PaymentId = @PaymentId;
    RETURN 0;  -- Éxito
END
```

---

### EJEMPLO 6: Paginación

**Uso**: Obtener solo un "página" de registros.

```csharp
public async Task<(List<PaymentResponseDto> payments, int totalRecords)> 
    GetPaymentsPagedAsync(Guid customerId, int pageNumber, int pageSize)
{
    var payments = new List<PaymentResponseDto>();

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
        payments.Add(new PaymentResponseDto { /* ... */ });
    }

    int totalRecords = (int?)totalRecordsParam.Value ?? 0;
    return (payments, totalRecords);
}
```

**SQL Procedure**:
```sql
CREATE PROCEDURE sp_GetPaymentsPaged
    @CustomerId UNIQUEIDENTIFIER,
    @PageNumber INT,
    @PageSize INT,
    @TotalRecords INT OUTPUT
AS
BEGIN
    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;
    
    SELECT @TotalRecords = COUNT(*)
    FROM Payments
    WHERE CustomerId = @CustomerId;
    
    SELECT PaymentId, CustomerId, ServiceProvider, Amount, Status, CreatedAt
    FROM Payments
    WHERE CustomerId = @CustomerId
    ORDER BY CreatedAt DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
```

---

### EJEMPLO 7: Parámetros Opcionales (NULL)

**Uso**: Filtros dinámicos donde algunos parámetros pueden ser NULL.

```csharp
public async Task<List<PaymentResponseDto>> GetPaymentsWithFiltersAsync(
    Guid? customerId = null,
    string? serviceProvider = null,
    decimal? minAmount = null,
    decimal? maxAmount = null)
{
    var payments = new List<PaymentResponseDto>();

    using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();

    using var command = new SqlCommand("sp_GetPaymentsFiltered", connection)
    {
        CommandType = System.Data.CommandType.StoredProcedure
    };

    // IMPORTANTE: Usar DBNull.Value si el parámetro es null
    command.Parameters.AddWithValue("@CustomerId", customerId ?? (object)DBNull.Value);
    command.Parameters.AddWithValue("@ServiceProvider", serviceProvider ?? (object)DBNull.Value);
    command.Parameters.AddWithValue("@MinAmount", minAmount ?? (object)DBNull.Value);
    command.Parameters.AddWithValue("@MaxAmount", maxAmount ?? (object)DBNull.Value);

    using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        payments.Add(new PaymentResponseDto { /* ... */ });
    }

    return payments;
}
```

**SQL Procedure**:
```sql
CREATE PROCEDURE sp_GetPaymentsFiltered
    @CustomerId UNIQUEIDENTIFIER = NULL,
    @ServiceProvider NVARCHAR(255) = NULL,
    @MinAmount DECIMAL(10, 2) = NULL,
    @MaxAmount DECIMAL(10, 2) = NULL
AS
BEGIN
    SELECT PaymentId, CustomerId, ServiceProvider, Amount, Status, CreatedAt
    FROM Payments
    WHERE 
        (@CustomerId IS NULL OR CustomerId = @CustomerId)
        AND (@ServiceProvider IS NULL OR ServiceProvider LIKE '%' + @ServiceProvider + '%')
        AND (@MinAmount IS NULL OR Amount >= @MinAmount)
        AND (@MaxAmount IS NULL OR Amount <= @MaxAmount)
    ORDER BY CreatedAt DESC;
END
```

---

### EJEMPLO 8: Transacciones

**Uso**: Ejecutar múltiples SPs dentro de una transacción (todo o nada).

```csharp
public async Task<bool> TransferPaymentAsync(Guid sourcePaymentId, Guid targetCustomerId)
{
    SqlTransaction transaction = null;
    try
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Iniciar transacción
        transaction = connection.BeginTransaction();

        // SP 1: Eliminar pago original
        using var deleteCommand = new SqlCommand("sp_DeletePayment", connection, transaction)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        deleteCommand.Parameters.AddWithValue("@PaymentId", sourcePaymentId);
        await deleteCommand.ExecuteNonQueryAsync();

        // SP 2: Crear nuevo pago para otro cliente
        using var createCommand = new SqlCommand("sp_CreatePaymentForCustomer", connection, transaction)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        createCommand.Parameters.AddWithValue("@TargetCustomerId", targetCustomerId);
        createCommand.Parameters.AddWithValue("@SourcePaymentId", sourcePaymentId);
        await createCommand.ExecuteNonQueryAsync();

        // Confirmar
        await transaction.CommitAsync();
        return true;
    }
    catch (Exception ex)
    {
        // Revertir en caso de error
        if (transaction != null)
        {
            await transaction.RollbackAsync();
        }
        throw;
    }
}
```

---

### EJEMPLO 9: Exportar como XML

**Uso**: El SP devuelve datos en formato XML.

```csharp
public async Task<string> ExportPaymentsSummaryAsXmlAsync(
    Guid customerId, 
    DateTime startDate, 
    DateTime endDate)
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
    return result?.ToString() ?? string.Empty;
}
```

**SQL Procedure**:
```sql
CREATE PROCEDURE sp_ExportPaymentsSummary
    @CustomerId UNIQUEIDENTIFIER,
    @StartDate DATETIME2,
    @EndDate DATETIME2
AS
BEGIN
    SELECT (
        SELECT 
            PaymentId,
            CustomerId,
            ServiceProvider,
            Amount,
            Status,
            CreatedAt
        FROM Payments
        WHERE CustomerId = @CustomerId
            AND CreatedAt >= @StartDate
            AND CreatedAt <= @EndDate
        FOR XML PATH('Payment'), ROOT('Payments')
    );
END
```

---

### EJEMPLO 10: Manejo de Errores

**Uso**: El SP lanza excepciones para errores críticos.

```csharp
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
        return rowsAffected > 0;
    }
    catch (SqlException ex)
    {
        // El SP lanzó un error
        _logger.LogError(ex, "SQL Error: {Message}", ex.Message);
        throw;
    }
}
```

**SQL Procedure**:
```sql
CREATE PROCEDURE sp_UpdatePaymentStatus
    @PaymentId UNIQUEIDENTIFIER,
    @NewStatus NVARCHAR(50),
    @UpdatedAt DATETIME2
AS
BEGIN
    UPDATE Payments
    SET Status = @NewStatus
    WHERE PaymentId = @PaymentId;
    
    IF @@ROWCOUNT = 0
    BEGIN
        THROW 50001, 'Payment not found', 1;  -- Lanza excepción
    END
END
```

---

## 📊 Tabla Comparativa: Métodos de Ejecución

| Método | Uso | Retorna |
|--------|-----|---------|
| `ExecuteNonQueryAsync()` | INSERT, UPDATE, DELETE | # de filas afectadas |
| `ExecuteScalarAsync()` | Un solo valor (SELECT COUNT, SUM, etc.) | Primer valor del resultado |
| `ExecuteReaderAsync()` | Múltiples filas | DataReader para iterar |

---

## ⚠️ Mejores Prácticas

### 1. **Siempre usa parámetros**
```csharp
// ✅ BIEN - Previene SQL injection
command.Parameters.AddWithValue("@Id", id);

// ❌ MALO - SQL injection
var query = $"SELECT * FROM Payments WHERE PaymentId = '{id}'";
```

### 2. **Especifica el tipo de dato**
```csharp
// ✅ BIEN - Explícito
var param = new SqlParameter("@Amount", SqlDbType.Decimal)
{
    Value = 150.50,
    Precision = 10,
    Scale = 2
};
command.Parameters.Add(param);

// ✅ ACEPTABLE - Para casos simples
command.Parameters.AddWithValue("@Amount", 150.50);
```

### 3. **Maneja valores NULL correctamente**
```csharp
// ✅ BIEN
command.Parameters.AddWithValue("@Optional", value ?? (object)DBNull.Value);

// ❌ MALO
command.Parameters.AddWithValue("@Optional", value);  // Si es null, falla
```

### 4. **Usa `using` para recursos**
```csharp
// ✅ BIEN - Se libera automáticamente
using var connection = new SqlConnection(_connectionString);
using var command = new SqlCommand("sp_Name", connection);

// ❌ MALO - Leak de recursos
var connection = new SqlConnection(_connectionString);
```

### 5. **Maneja excepciones apropiadamente**
```csharp
try
{
    await command.ExecuteNonQueryAsync();
}
catch (SqlException ex)
{
    _logger.LogError(ex, "Database error");
    throw;  // Relanzar o retornar error
}
```

---

## 🚀 Cómo Usar Esta Guía

1. **Revisar el archivo**: `PaymentRepositoryStoredProcExamples.cs`
2. **Ver los scripts SQL**: `StoredProcedures_Examples.sql`
3. **Adaptar a tu proyecto**:
   - Cambiar nombres de SPs
   - Ajustar parámetros según tu BD
   - Copiar el patrón que necesites

4. **Ejecutar los scripts SQL** en tu SQL Server primero
5. **Llamar desde el Controller**:
```csharp
[HttpPost("create")]
public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto)
{
    var payment = await _paymentService.CreatePaymentAsync(dto);
    return CreatedAtAction(nameof(GetPayments), new { customerId = payment.CustomerId }, payment);
}
```

---

## 📝 Checklist para un Nuevo SP

- [ ] Crear el SP en SQL Server con validaciones
- [ ] Probar el SP manualmente en SQL Server Management Studio
- [ ] Crear el método C# en el repositorio
- [ ] Agregar logging
- [ ] Manejar excepciones
- [ ] Documentar el método
- [ ] Crear tests unitarios

---

## 🔗 Referencias

- **Microsoft Docs**: [SqlCommand.CommandType](https://learn.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlcommand.commandtype)
- **SQL Server**: [CREATE PROCEDURE](https://learn.microsoft.com/en-us/sql/t-sql/statements/create-procedure-transact-sql)
- **ADO.NET Best Practices**: [Microsoft Docs](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/ado-net-overview)

---

**Última actualización**: 2026-09-28  
**Versión**: 1.0
