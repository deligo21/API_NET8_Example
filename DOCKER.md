# Instrucciones Docker para Payment API

## Opción 1: Docker Compose (Recomendado)

Usa Docker Compose para levantar tanto SQL Server como la API en contenedores.

### Requisitos
- Docker Desktop instalado

### Pasos

1. Navegar al directorio del proyecto:
```bash
cd D:\Chanchullo
```

2. Levantar los servicios:
```bash
docker-compose up -d
```

3. Verificar que los servicios estén corriendo:
```bash
docker-compose ps
```

4. Acceder a la API:
- Swagger: `https://localhost:5001/swagger/ui.html`
- Health Check: `https://localhost:5001/health`

5. Detener los servicios:
```bash
docker-compose down
```

6. Ver logs de los servicios:
```bash
docker-compose logs -f api
docker-compose logs -f sqlserver
```

## Opción 2: Docker (Manual)

### Paso 1: Levantar SQL Server

```bash
docker run -d `
  --name payment-api-sqlserver `
  -e SA_PASSWORD=YourPassword123! `
  -e ACCEPT_EULA=Y `
  -p 1433:1433 `
  -v sqlserver_data:/var/opt/mssql `
  mcr.microsoft.com/mssql/server:2022-latest
```

### Paso 2: Construir la imagen de la API

```bash
cd D:\Chanchullo
docker build -t payment-api .
```

### Paso 3: Ejecutar la API

```bash
docker run -d `
  --name payment-api `
  -p 5001:5001 `
  -p 5000:5000 `
  -e ConnectionStrings__DefaultConnection="Server=payment-api-sqlserver;Database=PaymentDB;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true;" `
  --link payment-api-sqlserver:sqlserver `
  payment-api
```

### Paso 4: Verificar que esté funcionando

```bash
docker logs payment-api
```

### Comandos útiles

```bash
# Ver todos los contenedores
docker ps -a

# Detener un contenedor
docker stop payment-api

# Eliminar contenedores
docker rm payment-api payment-api-sqlserver

# Eliminar volúmenes
docker volume rm sqlserver_data

# Ver logs en tiempo real
docker logs -f payment-api
```

## Conexión a SQL Server desde el Host

Si necesitas conectarte a SQL Server desde tu máquina local:

```
Server: localhost,1433
User: sa
Password: YourPassword123!
Database: PaymentDB
```

En SQL Server Management Studio (SSMS):
- Server name: `localhost,1433`
- Authentication: SQL Server Authentication
- Login: `sa`
- Password: `YourPassword123!`

## Troubleshooting

### Puerto 1433 ya está en uso
```bash
# Encontrar qué proceso usa el puerto
netstat -ano | findstr :1433

# Cambiar el puerto en docker-compose.yml o en el comando docker run
-p 1434:1433  # Usar puerto 1434 del host
```

### Error de conexión en la API
- Asegurarse de que SQL Server esté ejecutándose
- Verificar la connection string
- Revisar los logs del contenedor

### Limpiar todo
```bash
docker-compose down -v
```

## Variables de Entorno

Puedes sobrescribir variables en el comando de ejecución:

```bash
docker run -e ConnectionStrings__DefaultConnection="..." -e ASPNETCORE_ENVIRONMENT="Production" payment-api
```

## Acceder a la API en Docker

- **HTTP:** `http://localhost:5000`
- **HTTPS:** `https://localhost:5001`
- **Swagger:** `https://localhost:5001/swagger/ui.html`
