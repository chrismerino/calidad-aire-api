# API de calidad del aire

API para registrar sensores, guardar lecturas de calidad del aire y generar alertas. Está hecha en .NET 10 y usa MySQL en Docker. El acceso a los datos se protege con JWT.

## Antes de empezar

Necesitas .NET SDK 10 y Docker Desktop. Primero copia los archivos de ejemplo desde la carpeta del proyecto:

```bash
cp .env.example .env
cp CalidadAire.Api/appsettings.Local.example.json CalidadAire.Api/appsettings.Local.json
```

En `.env` escribe las contraseñas de MySQL. Luego abre `appsettings.Local.json`, usa la misma contraseña de `MYSQL_PASSWORD` en la conexión y escribe el usuario que usarás para entrar.

Ejecuta este comando y escribe la contraseña de acceso cuando te la pida:

```bash
dotnet run --project CalidadAire.Api --no-launch-profile -- --generar-hash
```

Guarda el resultado en `Acceso:ContrasenaHash`. Para la clave JWT, ejecuta `openssl rand -hex 32` y copia el resultado en `Jwt:Key`. Estos archivos locales no se suben a Git.

## Ejecutar

```bash
docker compose up -d --wait
dotnet run --project CalidadAire.Api --launch-profile http
```

Cuando la API esté ejecutándose, abre `http://localhost:5188/swagger` para probar las solicitudes.

Empieza con `POST /api/auth/login`, usando el usuario y la contraseña que configuraste. Después copia el token y pégalo en **Authorize**, sin escribir `Bearer`.

## Uso

- `/api/sensores`: registrar y consultar sensores.
- `/api/sensores/{id}/estado`: activar o desactivar con PATCH.
- `/api/lecturas`: registrar y consultar lecturas.
- `/api/alertas`: consultar alertas.
- `/api/lecturas/{id}/enriquecida`: consultar una lectura con clima actual.

Cada lectura lleva `sensorId`, `pm2_5`, `pm10`, `co2` y `fechaHora`. Usa fechas con `Z` u offset. Los valores no pueden ser negativos y el sensor debe estar activo.

Para filtrar lecturas usa `desde`, `hasta`, `sensorId` y `contaminante` (`PM2_5`, `PM10` o `CO2`). También acepta `pagina` y `tamano`. Se guarda una sola alerta por lectura, con el nivel de mayor gravedad.

El clima usa las coordenadas de `Clima:Latitud` y `Clima:Longitud`. Tiene su propia fecha y corresponde al clima actual.

El script `sql/01_tablas.sql` crea las tablas y relaciones. Docker lo ejecuta al iniciar un volumen vacío. MySQL escucha en `127.0.0.1:3308` y conserva los datos en el volumen.
