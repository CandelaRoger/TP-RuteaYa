# TP-RuteaYa

RuteaYa es una aplicación web de logística colaborativa desarrollada con ASP.NET Core. Permite a los usuarios publicar viajes, crear envíos, encontrar oportunidades compatibles con sus recorridos y calcular rutas y desvíos.

El sistema está dividido en una API REST protegida con JWT y una aplicación MVC que consume dicha API desde una interfaz web.

## Características principales

- Registro e inicio de sesión de usuarios.
- Autenticación mediante JSON Web Tokens.
- Revocación de tokens.
- Creación y administración de viajes.
- Publicación y seguimiento de envíos.
- Asociación de envíos con viajes.
- Cálculo de desvíos y rutas.
- Optimización del recorrido según las paradas y los envíos.
- Sugerencias y geocodificación de direcciones mediante HERE Maps.
- Servicio de rutas simulado cuando no se configura HERE Maps.
- Visualización de oportunidades disponibles y asignadas.
- Consulta de ganancias mensuales.
- Panel con información general de viajes y envíos.
- Pruebas unitarias e integración de controladores y servicios.

## Tecnologías utilizadas

- C# y .NET 10
- ASP.NET Core MVC
- ASP.NET Core Web API
- Entity Framework Core 10
- SQL Server
- JWT Bearer Authentication
- ASP.NET Core Identity con JWT
- BCrypt.Net-Next
- AutoMapper
- Swagger/OpenAPI
- HERE Maps REST API
- xUnit
- Moq
- Entity Framework Core InMemory
- Bootstrap
- jQuery
- Razor Views
- HTML, CSS y JavaScript

## Arquitectura del proyecto

La solución sigue una organización por responsabilidades:

```text
TP-RuteaYa
├── RuteaYa.Entidades
├── RuteaYa.DTOs
├── RuteaYa.Logica
│   └── Rutas
├── RuteaYa.WebApi
│   └── Controllers
├── RuteaYa.MVC
│   ├── Controllers
│   ├── Views
│   ├── Models
│   ├── Handlers
│   └── Mappings
├── RuteaYa.Tests
│   ├── ApiControllers
│   └── Servicios
└── RuteaYa.slnx
```

### RuteaYa.Entidades

Contiene las entidades persistidas por Entity Framework Core:

- `Usuario`
- `Viaje`
- `ViajeDia`
- `Parada`
- `Envio`
- `TokenRevocado`

Además, contiene las relaciones y claves foráneas utilizadas por `RuteaYaDbContext`.

### RuteaYa.DTOs

Contiene los objetos de transferencia utilizados para comunicar la API con la aplicación MVC.

Incluye DTOs para autenticación, registro, usuarios, viajes, envíos, oportunidades, rutas, ganancias mensuales y dashboards.

### RuteaYa.Logica

Contiene la lógica de negocio y el acceso a datos:

- Servicios de autenticación.
- Administración de viajes.
- Administración de envíos.
- Oportunidades de transporte.
- Dashboard.
- Ganancias mensuales.
- Gestión de tokens.
- Conexión con Entity Framework Core.
- Migraciones de base de datos.
- Servicios de rutas.
- Optimizador de recorridos.
- Integración con HERE Maps.

Los principales servicios son:

- `AuthService`
- `ViajeService`
- `EnvioService`
- `OportunidadService`
- `DashboardService`
- `GananciasMensualesService`
- `TokenService`
- `TransporteService`

### RuteaYa.WebApi

Expone los recursos HTTP de RuteaYa mediante controladores REST.

Los principales controladores son:

- `AuthApiController`
- `ViajesApiController`
- `EnviosApiController`
- `OportunidadesApiController`
- `TransportesApiController`
- `DashboardController`
- `GananciasMensualesApiController`
- `RutasController`

La API utiliza:

- JWT Bearer para autenticación.
- Claims para identificar al usuario autenticado.
- Entity Framework Core para SQL Server.
- Swagger para documentación interactiva.
- HERE Maps o el servicio de rutas simulado.

### RuteaYa.MVC

Implementa la interfaz web mediante ASP.NET Core MVC.

Contiene vistas para:

- Inicio de sesión y registro.
- Dashboard.
- Administración de viajes.
- Administración de envíos.
- Transporte disponible.
- Oportunidades.
- Ganancias mensuales.

La aplicación MVC consume `RuteaYa.WebApi` mediante `HttpClient`. El `BearerTokenHandler` agrega el JWT almacenado en la sesión a las solicitudes HTTP.

### RuteaYa.Tests

Contiene pruebas desarrolladas con:

- xUnit.
- Moq.
- Entity Framework Core InMemory.
- Coverlet.

Las pruebas están organizadas en:

- `ApiControllers`: pruebas de controladores de la API.
- `Servicios`: pruebas de servicios de la capa lógica.

## Flujo general

1. El usuario accede a la aplicación MVC.
2. El MVC solicita el inicio de sesión a la API.
3. La API valida las credenciales con `AuthService`.
4. La API genera un JWT.
5. El MVC guarda la cookie y utiliza el JWT mediante `BearerTokenHandler`.
6. Las solicitudes HTTP enviadas al API incluyen el token.
7. Los controladores extraen el identificador del usuario desde los claims.
8. La capa lógica ejecuta las reglas de negocio.
9. Entity Framework Core persiste o consulta la información en SQL Server.

## Requisitos

Antes de ejecutar el proyecto necesitas:

- Windows recomendado para SQL Server local.
- .NET SDK 10.
- SQL Server.
- SQL Server Management Studio o otra herramienta compatible, opcional.
- Visual Studio 2022 actualizado o Visual Studio Code.
- Una API key de HERE Maps, opcional.

Para verificar la instalación de .NET:

```bash
dotnet --version
```

## Configuración de SQL Server

La conexión predeterminada está configurada en `RuteaYa.WebApi/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=RuteaYaDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

Esta configuración utiliza:

- SQL Server local.
- Autenticación integrada de Windows.
- Base de datos `RuteaYaDB`.
- Certificados de SQL Server sin validación.

Si utilizas otro servidor o autenticación, modifica `DefaultConnection` mediante variables de entorno o User Secrets.

## Configuración de JWT

La clave utilizada para firmar los tokens se encuentra en:

```text
RuteaYa.WebApi/appsettings.json
```

Dentro de la sección:

```json
"JwtSettings": {
  "SecretKey": "...",
  "Issuer": "RuteaYaApi",
  "Audience": "RuteaYaMvc",
  "ExpireMinutes": "120"
}
```

Por seguridad, no publiques una clave real en el repositorio ni la utilices directamente en producción. Reemplaza la clave mediante User Secrets o variables de entorno.

Ejemplo usando User Secrets:

```bash
dotnet user-secrets init --project RuteaYa.WebApi
dotnet user-secrets set "JwtSettings:SecretKey" "TU_CLAVE_SECRETA_SEGURA" --project RuteaYa.WebApi
```

La clave debe tener una longitud suficiente para el algoritmo de firma utilizado por la aplicación.

## Configuración de HERE Maps

HERE Maps es opcional. Si no se proporciona una API key, el sistema utiliza `MockRutaService` y puede continuar funcionando con datos simulados.

Para configurar HERE Maps:

```bash
dotnet user-secrets init --project RuteaYa.WebApi
dotnet user-secrets set "Here:ApiKey" "TU_API_KEY" --project RuteaYa.WebApi
```

También puedes configurar una variable de entorno llamada:

```text
Here__ApiKey
```

Cuando existe `Here:ApiKey`, `HereRutaService` se registra como implementación de `IRutaService`.

Este servicio permite:

- Buscar sugerencias de direcciones.
- Geocodificar una dirección.
- Calcular rutas entre coordenadas.
- Obtener distancia y duración.
- Decodificar la polyline entregada por HERE Maps.

## Configuración de la aplicación MVC

`RuteaYa.MVC/appsettings.json` contiene la dirección de la API:

```json
"ApiSettings": {
  "BaseUrl": "https://localhost:7086"
}
```

Puedes modificarla con:

```bash
dotnet user-secrets set "ApiSettings:BaseUrl" "https://localhost:7086" --project RuteaYa.MVC
```

También puede utilizarse una variable de entorno:

```text
ApiSettings__BaseUrl
```

## Restaurar dependencias

Desde la raíz del repositorio:

```bash
dotnet restore RuteaYa.slnx
```

## Crear la base de datos

Primero debes asegurarte de que SQL Server esté ejecutándose.

Luego puedes aplicar las migraciones desde la raíz del proyecto:

```bash
dotnet tool restore
dotnet ef database update \
  --project RuteaYa.Logica \
  --startup-project RuteaYa.WebApi
```

Si necesitas instalar EF Core CLI:

```bash
dotnet tool install --global dotnet-ef
```

Las migraciones disponibles incluyen la creación inicial y cambios posteriores para:

- Usuarios.
- Viajes.
- Días de viaje.
- Paradas.
- Envios.
- Relaciones entre viajes y envíos.
- Desvíos.
- Tokens revocados.

## Ejecutar la API

En una terminal:

```bash
dotnet run --project RuteaYa.WebApi
```

Por defecto, los puertos pueden consultarse en:

```text
RuteaYa.WebApi/Properties/launchSettings.json
```

En modo desarrollo, Swagger estará disponible en una dirección similar a:

```text
https://localhost:7086/swagger
```

## Ejecutar la aplicación web MVC

En una segunda terminal:

```bash
dotnet run --project RuteaYa.MVC
```

La interfaz se abrirá en la URL indicada por `launchSettings.json`, normalmente en:

```text
https://localhost:PORT
```

Se debe iniciar primero la API y luego el MVC.

## Ejecutar ambos proyectos desde Visual Studio

1. Abre `RuteaYa.slnx`.
2. Haz clic derecho sobre la solución.
3. Selecciona **Propiedades**.
4. Configura **Varios proyectos de inicio**.
5. Cambia a iniciar:
   - `RuteaYa.WebApi`.
   - `RuteaYa.MVC`.
6. Presiona `F5`.

## Uso básico

### Registro

Crea una cuenta desde la vista de registro.

La contraseña se transforma en un hash BCrypt antes de almacenarse.

### Inicio de sesión

Inicia sesión desde `RuteaYa.MVC/Views/Auth/Login.cshtml`.

La API valida las credenciales y devuelve un token JWT.

### Crear un viaje

Los conductores pueden crear viajes indicando:

- Dirección de origen.
- Dirección de destino.
- Coordenadas.
- Hora de salida.
- Hora de llegada.
- Cantidad máxima de paquetes.
- Peso máximo.
- Desvío máximo.
- Distancia.
- Duración.
- Días de la semana.
- Paradas.

### Publicar un envío

Los remitentes pueden publicar envíos especificando:

- Dirección de origen.
- Dirección de destino.
- Fecha.
- Peso.
- Tamaño.
- Descripción.
- Monto ofrecido.
- Desvío estimado.

### Consultar oportunidades

El sistema permite visualizar oportunidades compatibles con los viajes existentes.

Una oportunidad puede incluir información sobre:

- Desvío requerido.
- Peso disponible.
- Capacidad del viaje.
- Estado del envío.
- Monto ofrecido.

### Completar una ruta

El optimizador puede explorar distintas combinaciones de:

- Paradas originales.
- Retiros de envíos.
- Entregas de envíos.

Selecciona la combinación con menor distancia calculada.

## Base de datos

La entidad `Usuario` tiene una relación uno a muchos con:

- Viajes.
- Envíos.

La entidad `Viaje` tiene relaciones con:

- Usuario propietario.
- Días de operación.
- Paradas.
- Envíos asociados.

La entidad `Envio` contiene:

- Usuario creador.
- Viaje asignado.
- Origen y destino.
- Coordenadas.
- Peso y tamaño.
- Estado.
- Monto ofrecido.
- Desvío.

Las eliminaciones están configuradas para evitar borrar usuarios que tengan datos asociados.

## API REST

Todos los endpoints relacionados con recursos protegidos requieren:

```http
Authorization: Bearer TU_JWT
```

Ejemplos de rutas disponibles:

```text
POST   /api/AuthApi/Login
POST   /api/AuthApi/Registro
GET    /api/Viajes
GET    /api/Viajes/{id}
POST   /api/Viajes
PUT    /api/Viajes/{id}
PATCH  /api/Viajes/{id}/desactivar
GET    /api/Viajes/{viajeId}/envios/{envioId}/desvio
GET    /api/Envios
POST   /api/Envios
GET    /api/Oportunidades
GET    /api/Dashboard
GET    /api/GananciasMensuales
GET    /api/Rutas
```

Las rutas exactas pueden variar según las acciones y atributos de cada controlador. La forma más completa de consultarlas es abrir Swagger.

## Ejecutar pruebas

Desde la raíz:

```bash
dotnet test RuteaYa.Tests/RuteaYa.Tests.csproj
```

Para ejecutar todas las pruebas de la solución:

```bash
dotnet test RuteaYa.slnx
```

## Compilar el proyecto

```bash
dotnet build RuteaYa.slnx
```

## Crear una compilación publicada

API:

```bash
dotnet publish RuteaYa.WebApi/RuteaYa.WebApi.csproj -c Release -o ./publish/api
```

MVC:

```bash
dotnet publish RuteaYa.MVC/RuteaYa.MVC.csproj -c Release -o ./publish/mvc
```

## Migraciones

Para crear una nueva migración:

```bash
dotnet ef migrations add NombreDeLaMigracion \
  --project RuteaYa.Logica \
  --startup-project RuteaYa.WebApi
```

Para aplicar migraciones:

```bash
dotnet ef database update \
  --project RuteaYa.Logica \
  --startup-project RuteaYa.WebApi
```

Para eliminar la última migración no aplicada:

```bash
dotnet ef migrations remove \
  --project RuteaYa.Logica \
  --startup-project RuteaYa.WebApi
```

## Seguridad

Antes de desplegar RuteaYa:

- No versionar claves JWT reales.
- No versionar API keys de HERE.
- Utilizar HTTPS.
- Configurar secretos mediante variables de entorno o User Secrets.
- Utilizar una clave JWT larga y aleatoria.
- Cambiar la clave predeterminada de `appsettings.json`.
- Activar validación de issuer y audience si se necesitan en el entorno productivo.
- Utilizar secretos externos en producción.
- No habilitar Swagger en producción a menos que esté protegido.
- Cambiar la cadena de conexión por una configuración segura.
- Revocar tokens comprometidos.
- Validar las entradas recibidas por los endpoints.

## Mejoras posibles

- Agregar contenedores Docker y Docker Compose.
- Agregar archivos `.env.example`.
- Incorporar una configuración centralizada de URLs.
- Agregar health checks.
- Agregar logging estructurado.
- Implementar caché.
- Incorporar control de roles.
- Implementar paginación.
- Incorporar un proveedor de mapas configurable.
- Agregar pruebas E2E.
- Incorporar integración continua.
- Definir documentación OpenAPI persistente.
- Agregar un administrador de migraciones al arranque.
- Evitar almacenar secretos en `appsettings.json`.

## Solución de problemas

### Error de conexión a SQL Server

Comprueba que:

- SQL Server esté iniciado.
- La instancia `.` esté disponible.
- El usuario de Windows tenga permisos.
- La cadena `DefaultConnection` sea correcta.
- `TrustServerCertificate=True` esté permitido.

### La aplicación MVC no puede comunicarse con la API

Comprueba:

- Que ambos proyectos estén ejecutándose.
- Que `ApiSettings:BaseUrl` tenga el puerto correcto.
- Que el certificado HTTPS sea confiable.
- Que no exista un firewall bloqueando la API.

### HERE Maps no está disponible

Si no existe `Here:ApiKey`, el sistema utilizará `MockRutaService`.

Si existe una clave inválida:

- Comprueba que la clave sea válida.
- Comprueba que tenga acceso a los servicios utilizados.
- Comprueba la conexión a Internet.

### El token JWT no es aceptado

Comprueba:

- Que el token se envíe en el header `Authorization`.
- Que use el formato `Bearer <token>`.
- Que la clave y el algoritmo sean coherentes.
- Que el token no haya sido revocado.
- Que no haya expirado.

### No se encuentra una dirección

Asegúrate de:

- Tener una API key válida.
- Incluir suficiente contexto en la dirección.
- Permitir búsquedas regionales.
- Tener conectividad con HERE Maps.

## Contribuir

1. Realiza un fork del repositorio.
2. Crea una rama descriptiva:

```bash
git checkout -b feature/nueva-funcionalidad
```

3. Realiza y prueba los cambios.

```bash
dotnet test RuteaYa.slnx
```

4. Confirma los cambios:

```bash
git add .
git commit -m "Agrega nueva funcionalidad"
```

5. Envía la rama:

```bash
git push origin feature/nueva-funcionalidad
```

6. Abre un Pull Request.

