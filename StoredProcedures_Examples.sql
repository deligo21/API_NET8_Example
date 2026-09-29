-- ============================================================================
-- STORED PROCEDURES - EJEMPLOS PARA PAYMENT API
-- Ejecutar estos scripts en SQL Server para crear los Stored Procedures
-- ============================================================================

-- ============================================================================
-- EJEMPLO 1: Crear un pago
-- ============================================================================
CREATE PROCEDURE sp_CreatePayment
    @PaymentId UNIQUEIDENTIFIER,
    @CustomerId UNIQUEIDENTIFIER,
    @ServiceProvider NVARCHAR(255),
    @Amount DECIMAL(10, 2),
    @Status NVARCHAR(50),
    @CreatedAt DATETIME2
AS
BEGIN
    BEGIN TRY
        INSERT INTO Payments (PaymentId, CustomerId, ServiceProvider, Amount, Status, CreatedAt)
        VALUES (@PaymentId, @CustomerId, @ServiceProvider, @Amount, @Status, @CreatedAt);

        RETURN 0; -- Éxito
    END TRY
    BEGIN CATCH
        THROW; -- Relanzar el error
    END CATCH
END
GO

-- ============================================================================
-- EJEMPLO 2: Obtener pagos por cliente con contador
-- ============================================================================
CREATE PROCEDURE sp_GetPaymentsByCustomer
    @CustomerId UNIQUEIDENTIFIER,
    @TotalCount INT OUTPUT
AS
BEGIN
    BEGIN TRY
        -- Contar el total de pagos
        SELECT @TotalCount = COUNT(*)
        FROM Payments
        WHERE CustomerId = @CustomerId;

        -- Devolver los pagos ordenados por fecha
        SELECT PaymentId, CustomerId, ServiceProvider, Amount, Status, CreatedAt
        FROM Payments
        WHERE CustomerId = @CustomerId
        ORDER BY CreatedAt DESC;

        RETURN 0; -- Éxito
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-- ============================================================================
-- EJEMPLO 3: Actualizar el estado de un pago
-- ============================================================================
CREATE PROCEDURE sp_UpdatePaymentStatus
    @PaymentId UNIQUEIDENTIFIER,
    @NewStatus NVARCHAR(50),
    @UpdatedAt DATETIME2
AS
BEGIN
    BEGIN TRY
        UPDATE Payments
        SET Status = @NewStatus,
            CreatedAt = @UpdatedAt -- O usar una columna UpdatedAt si existe
        WHERE PaymentId = @PaymentId;

        IF @@ROWCOUNT = 0
        BEGIN
            THROW 50001, 'Payment not found', 1;
        END

        RETURN 0; -- Éxito
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-- ============================================================================
-- EJEMPLO 4: Obtener resumen de pagos (múltiples result sets)
-- ============================================================================
CREATE PROCEDURE sp_GetPaymentSummary
    @CustomerId UNIQUEIDENTIFIER
AS
BEGIN
    BEGIN TRY
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

        RETURN 0; -- Éxito
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-- ============================================================================
-- EJEMPLO 5: Procesar un pago (con código de retorno)
-- ============================================================================
CREATE PROCEDURE sp_ProcessPayment
    @PaymentId UNIQUEIDENTIFIER,
    @ProcessingMethod NVARCHAR(50),
    @ProcessedAt DATETIME2
AS
BEGIN
    BEGIN TRY
        DECLARE @PaymentExists BIT = 0;
        DECLARE @PaymentAmount DECIMAL(10, 2) = 0;

        -- Verificar que existe el pago
        SELECT @PaymentExists = 1, @PaymentAmount = Amount
        FROM Payments
        WHERE PaymentId = @PaymentId;

        IF @PaymentExists = 0
        BEGIN
            RETURN 1; -- Pago no encontrado
        END

        -- Validar que el monto sea procesable
        IF @PaymentAmount > 1500
        BEGIN
            RETURN 2; -- Monto excede límite
        END

        -- Procesar el pago
        UPDATE Payments
        SET Status = 'procesado'
        WHERE PaymentId = @PaymentId;

        -- Log de procesamiento (si existe tabla de auditoría)
        -- INSERT INTO PaymentLog (PaymentId, Action, ProcessingMethod, Timestamp)
        -- VALUES (@PaymentId, 'PROCESSED', @ProcessingMethod, @ProcessedAt);

        RETURN 0; -- Éxito
    END TRY
    BEGIN CATCH
        RETURN -1; -- Error general
    END CATCH
END
GO

-- ============================================================================
-- EJEMPLO 6: Obtener total de pagos (valor escalar)
-- ============================================================================
CREATE PROCEDURE sp_GetTotalPaymentsByCustomer
    @CustomerId UNIQUEIDENTIFIER
AS
BEGIN
    BEGIN TRY
        SELECT SUM(Amount)
        FROM Payments
        WHERE CustomerId = @CustomerId;

        RETURN 0; -- Éxito
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-- ============================================================================
-- EJEMPLO 7: Obtener pagos con paginación
-- ============================================================================
CREATE PROCEDURE sp_GetPaymentsPaged
    @CustomerId UNIQUEIDENTIFIER,
    @PageNumber INT,
    @PageSize INT,
    @TotalRecords INT OUTPUT
AS
BEGIN
    BEGIN TRY
        DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

        -- Contar total de registros
        SELECT @TotalRecords = COUNT(*)
        FROM Payments
        WHERE CustomerId = @CustomerId;

        -- Devolver página solicitada
        SELECT PaymentId, CustomerId, ServiceProvider, Amount, Status, CreatedAt
        FROM Payments
        WHERE CustomerId = @CustomerId
        ORDER BY CreatedAt DESC
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY;

        RETURN 0; -- Éxito
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-- ============================================================================
-- EJEMPLO 8: Exportar pagos como XML
-- ============================================================================
CREATE PROCEDURE sp_ExportPaymentsSummary
    @CustomerId UNIQUEIDENTIFIER,
    @StartDate DATETIME2,
    @EndDate DATETIME2
AS
BEGIN
    BEGIN TRY
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

        RETURN 0; -- Éxito
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-- ============================================================================
-- EJEMPLO 9A: Obtener pago por ID
-- ============================================================================
CREATE PROCEDURE sp_GetPaymentById
    @PaymentId UNIQUEIDENTIFIER
AS
BEGIN
    BEGIN TRY
        SELECT PaymentId, CustomerId, ServiceProvider, Amount, Status, CreatedAt
        FROM Payments
        WHERE PaymentId = @PaymentId;

        RETURN 0; -- Éxito
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-- ============================================================================
-- EJEMPLO 9B: Eliminar un pago
-- ============================================================================
CREATE PROCEDURE sp_DeletePayment
    @PaymentId UNIQUEIDENTIFIER
AS
BEGIN
    BEGIN TRY
        DELETE FROM Payments
        WHERE PaymentId = @PaymentId;

        IF @@ROWCOUNT = 0
        BEGIN
            THROW 50001, 'Payment not found', 1;
        END

        RETURN 0; -- Éxito
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-- ============================================================================
-- EJEMPLO 9C: Crear pago para otro cliente (usado en transfer)
-- ============================================================================
CREATE PROCEDURE sp_CreatePaymentForCustomer
    @TargetCustomerId UNIQUEIDENTIFIER,
    @SourcePaymentId UNIQUEIDENTIFIER
AS
BEGIN
    BEGIN TRY
        DECLARE @SourceAmount DECIMAL(10, 2);
        DECLARE @SourceProvider NVARCHAR(255);

        -- Obtener datos del pago original
        SELECT @SourceAmount = Amount, @SourceProvider = ServiceProvider
        FROM Payments
        WHERE PaymentId = @SourcePaymentId;

        -- Crear nuevo pago para el cliente destino
        INSERT INTO Payments (PaymentId, CustomerId, ServiceProvider, Amount, Status, CreatedAt)
        VALUES (NEWID(), @TargetCustomerId, @SourceProvider, @SourceAmount, 'pendiente', GETUTCDATE());

        RETURN 0; -- Éxito
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-- ============================================================================
-- EJEMPLO 10: Obtener pagos con filtros dinámicos
-- ============================================================================
CREATE PROCEDURE sp_GetPaymentsFiltered
    @CustomerId UNIQUEIDENTIFIER = NULL,
    @ServiceProvider NVARCHAR(255) = NULL,
    @MinAmount DECIMAL(10, 2) = NULL,
    @MaxAmount DECIMAL(10, 2) = NULL
AS
BEGIN
    BEGIN TRY
        SELECT PaymentId, CustomerId, ServiceProvider, Amount, Status, CreatedAt
        FROM Payments
        WHERE
            (@CustomerId IS NULL OR CustomerId = @CustomerId)
            AND (@ServiceProvider IS NULL OR ServiceProvider LIKE '%' + @ServiceProvider + '%')
            AND (@MinAmount IS NULL OR Amount >= @MinAmount)
            AND (@MaxAmount IS NULL OR Amount <= @MaxAmount)
        ORDER BY CreatedAt DESC;

        RETURN 0; -- Éxito
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

-- ============================================================================
-- SCRIPT DE VERIFICACIÓN
-- Ejecutar para ver los SPs creados
-- ============================================================================
-- SELECT * FROM INFORMATION_SCHEMA.ROUTINES
-- WHERE ROUTINE_TYPE = 'PROCEDURE'
-- AND ROUTINE_NAME LIKE 'sp_%'
-- ORDER BY ROUTINE_NAME;
