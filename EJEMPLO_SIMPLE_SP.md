# Ejemplos Simples: Convertir Tu Código Actual a Stored Procedures

Este documento muestra cómo reemplazar tu código SQL directo actual con Stored Procedures.

---

## 📌 Tu Código ACTUAL

Tu `PaymentRepository.cs` usa SQL directo:

```csharp
public async Task<Guid> CreatePaymentAsync(CreatePaymentDto dto)
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
    return paymentId;
}
```

---

## ✨ VERSIÓN MEJORADA con Stored Procedure

### PASO 1: Crear el Stored Procedure en SQL Server

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
GO
```

### PASO 2: Actualizar el Método en C#

Cambios principales:
- `CommandType = System.Data.CommandType.StoredProcedure`
- Nombre del SP en lugar de query SQL
- El resto es igual

```csharp
public async Task<Guid> CreatePaymentAsync(CreatePaymentDto dto)
{
    var paymentId = Guid.NewGuid();
    using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();

    // CAMBIO: Usar StoredProcedure en lugar de SQL directo
    using var command = new SqlCommand("sp_CreatePayment", connection)
    {
        CommandType = System.Data.CommandType.StoredProcedure  // ← AQUÍ
    };

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
```

---

## 📋 Comparación: SQL Directo vs Stored Procedure

### Tu Código ACTUAL (SQL Directo)

```csharp
public async Task<List<PaymentResponseDto>> GetPaymentsByCustomerIdAsync(Guid customerId)
{
    var payments = new List<PaymentResponseDto>();

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

    return payments;
}
```

### Versión MEJORADA (Stored Procedure)

```csharp
public async Task<List<PaymentResponseDto>> GetPaymentsByCustomerIdAsync(Guid customerId)
{
    var payments = new List<PaymentResponseDto>();

    using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();

    // CAMBIO: Especificar que es un Stored Procedure
    using var command = new SqlCommand("sp_GetPaymentsByCustomer", connection)
    {
        CommandType = System.Data.CommandType.StoredProcedure  // ← AQUÍ
    };

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

    return payments;
}
```

**SQL Procedure correspondiente**:

```sql
CREATE PROCEDURE sp_GetPaymentsByCustomer
    @CustomerId UNIQUEIDENTIFIER
AS
BEGIN
    SELECT PaymentId, CustomerId, ServiceProvider, Amount, Status, CreatedAt
    FROM Payments
    WHERE CustomerId = @CustomerId
    ORDER BY CreatedAt DESC;
END
GO
```

---

## 🔄 Pasos para Migrar Tu Repositorio Completo

### 1️⃣ Crear los Stored Procedures

Ejecutar en SQL Server:

```sql
-- SP 1: Crear pago
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
GO

-- SP 2: Obtener pagos por cliente
CREATE PROCEDURE sp_GetPaymentsByCustomer
    @CustomerId UNIQUEIDENTIFIER
AS
BEGIN
    SELECT PaymentId, CustomerId, ServiceProvider, Amount, Status, CreatedAt
    FROM Payments
    WHERE CustomerId = @CustomerId
    ORDER BY CreatedAt DESC;
END
GO
```

### 2️⃣ Actualizar `PaymentRepository.cs`

```csharp
// En ambos métodos, cambiar:

// ANTES:
string query = @"..."; 
using var command = new SqlCommand(query, connection);

// DESPUÉS:
using var command = new SqlCommand("sp_NombreProcedimiento", connection)
{
    CommandType = System.Data.CommandType.StoredProcedure
};
```

### 3️⃣ Probar

```bash
dotnet run
# Acceder a https://localhost:5001/swagger
# Hacer un POST /api/payments
# Hacer un GET /api/payments?customerId=...
```

---

## 💡 3 Razones Para Usar Stored Procedures

| Ventaja | Explicación | Ejemplo |
|---------|-------------|---------|
| **Rendimiento** | SQL precompilado en BD | Menos transferencia de datos |
| **Seguridad** | Lógica centralizada en BD | El usuario no ve el SQL |
| **Validaciones** | Validaciones en BD directamente | Lógica compartida entre apps |

---

## 📊 Checklist de Migración

- [ ] Crear Stored Procedure en SQL Server
- [ ] Cambiar `CommandType` a `StoredProcedure`
- [ ] Cambiar nombre del comando a nombre del SP
- [ ] Mantener parámetros igual
- [ ] Mantener mapeo de resultados igual
- [ ] Probar el endpoint
- [ ] Revisar logs

---

## 🎯 Próximos Pasos

1. **Copia** los Stored Procedures del archivo `StoredProcedures_Examples.sql`
2. **Ejecuta** en SQL Server Management Studio
3. **Reemplaza** solo las líneas de comando en tu repositorio
4. **Mantén** el resto del código igual

---

**¡Listo!** Ahora tu API usa Stored Procedures directamente. 🚀
