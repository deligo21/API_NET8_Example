# Quick Start - Payment API

## 🚀 Inicio Rápido (5 minutos)

### Opción A: Ejecutar localmente (sin Docker)

#### 1. Requisitos
- .NET 8 SDK instalado: https://dotnet.microsoft.com/en-us/download
- SQL Server o SQL Express instalado

#### 2. Configurar la base de datos

Editar `appsettings.json` con tu connection string:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=PaymentDB;Integrated Security=true;TrustServerCertificate=true;"
}
```

#### 3. Ejecutar
```powershell
cd D:\Chanchullo
dotnet run
```

La API estará en: `https://localhost:5001`
Swagger estará en: `https://localhost:5001/swagger`

---

### Opción B: Con Docker Compose (Recomendado)

#### 1. Requisitos
- Docker Desktop instalado

#### 2. Ejecutar
```bash
cd D:\Chanchullo
docker-compose up
```

La API estará en: `https://localhost:5001`

---

## 📝 Pruebas Rápidas

### 1. Crear un pago
```bash
curl -k -X POST https://localhost:5001/api/payments \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "cfe8b150-2f84-4ala-bdf4-923b20e34973",
    "serviceProvider": "SERVICIOS ELÉCTRICOS S.A.",
    "amount": 120.50
  }'
```

### 2. Consultar pagos
```bash
curl -k https://localhost:5001/api/payments?customerId=cfe8b150-2f84-4ala-bdf4-923b20e34973
```

### 3. Usar Swagger (Interfaz Gráfica)
1. Abrir `https://localhost:5001/swagger`
2. Hacer clic en "Try it out"
3. Llenar los campos
4. Hacer clic en "Execute"

---

## 📚 Documentación

- **README.md** - Documentación completa
- **DOCKER.md** - Instrucciones de Docker
- **requests.http** - Ejemplos para REST Client (VS Code)

---

## ⚠️ Problemas Comunes

### "Could not connect to database"
- ✓ Verificar que SQL Server esté ejecutándose
- ✓ Verificar la connection string en `appsettings.json`

### "Port 5001 already in use"
- Cambiar en `launchSettings.json` o ejecutar en otro puerto

### "Certificate validation failed"
- Agregar `-k` a curl o ignore certificate en Postman

---

## 🎯 Siguiente Pasos

1. Revisar el código en `Controllers/PaymentsController.cs`
2. Explorar la validación en `Services/PaymentService.cs`
3. Ver queries SQL en `Services/PaymentRepository.cs`
4. Modificar según tus necesidades

**¡Listo para usar!** 🎉
