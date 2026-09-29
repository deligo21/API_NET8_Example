# Payment API - Registrador de Pagos de Servicios Básicos

API REST desarrollada en .NET 8 para registrar y consultar pagos de servicios básicos (agua, electricidad, telecomunicaciones).

## Características

- ✅ Registrar pagos con validaciones
- ✅ Consultar pagos por cliente
- ✅ Persistencia en base de datos SQL Server sin Entity Framework
- ✅ Documentación interactiva con Swagger
- ✅ Manejo robusto de errores
- ✅ Arquitectura en capas (Controllers → Services → Repository)

## Requisitos

- .NET 8 SDK
- SQL Server (local o remoto)
- PowerShell o CMD para ejecutar los comandos

## Configuración

### 1. Clonar/Descargar el proyecto

```bash
cd D:\Chanchullo
```

### 2. Configurar la Connection String

Editar `appsettings.json` y reemplazar la connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PaymentDB;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true;"
  }
}
```

**Opciones de Connection String:**

- **SQL Server Local (Windows Auth):**
  ```
  Server=(localdb)\\mssqllocaldb;Database=PaymentDB;Integrated Security=true;TrustServerCertificate=true;
  ```

- **SQL Server Local (SA):**
  ```
  Server=localhost;Database=PaymentDB;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true;
  ```

- **SQL Server Remoto:**
  ```
  Server=your-server.database.windows.net;Database=PaymentDB;User Id=username;Password=password;
  ```

### 3. Instalar dependencias

```bash
dotnet restore
```

### 4. Ejecutar la API

```bash
dotnet run
```

La API estará disponible en `https://localhost:5001`

## Uso de la API

### 1. Registrar un Pago

**Endpoint:** `POST /api/payments`

**Request:**
```json
{
  "customerId": "cfe8b150-2f84-4ala-bdf4-923b20e34973",
  "serviceProvider": "SERVICIOS ELÉCTRICOS S.A.",
  "amount": 120.50
}
```

**Response (201 Created):**
```json
{
  "paymentId": "a248ad43-1f44-4b32-b0a0-e1c725b9bb7d",
  "customerId": "cfe8b150-2f84-4ala-bdf4-923b20e34973",
  "serviceProvider": "SERVICIOS ELÉCTRICOS S.A.",
  "amount": 120.50,
  "status": "pendiente",
  "createdAt": "2025-07-17T08:30:00Z"
}
```

**Validaciones:**
- `amount` no debe ser mayor a 1500 Bs
- `amount` debe estar en Bolivianos (Bs), no en dólares ($)
- `amount` debe ser mayor a 0
- `customerId` es requerido
- `serviceProvider` es requerido

**Errores:**
```json
// Amount > 1500
{
  "error": "Amount cannot exceed 1500 Bs"
}

// Moneda inválida
{
  "error": "Currency must be in Bolivianos (Bs), not dollars"
}
```

### 2. Consultar Pagos de un Cliente

**Endpoint:** `GET /api/payments?customerId=cfe8b150-2f84-4ala-bdf4-923b20e34973`

**Response (200 OK):**
```json
[
  {
    "paymentId": "a248ad43-1f44-4b32-b0a0-e1c725b9bb7d",
    "customerId": "cfe8b150-2f84-4ala-bdf4-923b20e34973",
    "serviceProvider": "SERVICIOS ELÉCTRICOS S.A.",
    "amount": 120.50,
    "status": "pendiente",
    "createdAt": "2025-07-17T08:30:00Z"
  }
]
```

## Documentación Interactiva

Una vez que la API está ejecutando, acceder a Swagger en:

```
https://localhost:5001/swagger/ui.html
```

## Estructura del Proyecto

```
PaymentAPI/
├── Controllers/
│   └── PaymentsController.cs      # Endpoints de la API
├── Services/
│   ├── PaymentService.cs          # Lógica de negocio
│   └── PaymentRepository.cs       # Acceso a datos (ADO.NET)
├── Models/
│   ├── CreatePaymentDto.cs        # DTO para crear pago
│   └── PaymentResponseDto.cs      # DTO de respuesta
├── Exceptions/
│   └── ValidationException.cs     # Excepciones personalizadas
├── Program.cs                     # Configuración de la aplicación
├── appsettings.json              # Configuración (connection string, logging)
├── PaymentAPI.csproj             # Proyecto .NET
└── README.md                      # Este archivo
```

## Estructura de la Base de Datos

La tabla `Payments` se crea automáticamente al iniciar la aplicación:

```sql
CREATE TABLE Payments (
    PaymentId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CustomerId UNIQUEIDENTIFIER NOT NULL,
    ServiceProvider NVARCHAR(255) NOT NULL,
    Amount DECIMAL(10, 2) NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'pendiente',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
)
```

## Ejemplo con cURL

### Registrar un pago:
```bash
curl -X POST https://localhost:5001/api/payments \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "cfe8b150-2f84-4ala-bdf4-923b20e34973",
    "serviceProvider": "SERVICIOS ELÉCTRICOS S.A.",
    "amount": 120.50
  }'
```

### Consultar pagos:
```bash
curl https://localhost:5001/api/payments?customerId=cfe8b150-2f84-4ala-bdf4-923b20e34973
```

## Logs

Los logs se muestran en consola durante la ejecución. Para cambiar el nivel de log, editar `appsettings.json`:

```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning"
  }
}
```

## Opcionales

### Docker

Crear un `Dockerfile` en la raíz del proyecto:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY . .
RUN dotnet build
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
EXPOSE 5001
ENTRYPOINT ["dotnet", "PaymentAPI.dll"]
```

Construir la imagen:
```bash
docker build -t payment-api .
```

Ejecutar el contenedor:
```bash
docker run -p 5001:5001 -e ConnectionStrings__DefaultConnection="Server=host.docker.internal;Database=PaymentDB;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true;" payment-api
```

### Kafka (Opcional)

Para agregar Kafka en el futuro:
1. Instalar el NuGet package: `Confluent.Kafka`
2. Crear un servicio `IPaymentEventPublisher`
3. Publicar eventos cuando se cree/modifique un pago

## Mejoras Futuras

- [ ] Autenticación JWT
- [ ] Filtros adicionales (por fecha, rango de montos)
- [ ] Paginación en consultas
- [ ] Cambiar estado de pagos
- [ ] Auditoría de cambios
- [ ] Integración con Kafka para eventos

## Soporte

Para preguntas o problemas, contactar al equipo de desarrollo.
