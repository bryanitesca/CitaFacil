# CitaFácil

Aplicación web para la gestión integral de citas médicas. Permite a pacientes reservar citas con un flujo guiado, a doctores administrar su agenda y al equipo administrativo supervisar usuarios, especialidades y respaldos de la base de datos.

## Características principales

- **Agenda para pacientes** con selección por especialidad, disponibilidad visual con calendario y validación de citas a partir del día siguiente.
- **Panel del doctor** con calendario FullCalendar, detalles de pacientes, historial de consultas y finalización con notas clínicas.
- **Dashboard de administración** con métricas, gestión de usuarios/especialidades y nuevo módulo de respaldos/restauración de la base de datos en formato `.bak`.
- **Notificaciones internas** entre pacientes y doctores sin dependencia de correo electrónico.
- Autenticación basada en cookies con roles (`Administrador`, `Doctor`, `Paciente`) y layouts específicos para cada rol.

## Tecnologías

- **Backend**: ASP.NET Core 9, Entity Framework Core (SQL Server).
- **Frontend**: Razor Pages, Tailwind CSS, FullCalendar, componentes estilizados con Material Symbols.
- **Base de datos**: Microsoft SQL Server (LocalDB, Express o instancia completa).
- **Otros**: Hosted services para tareas programadas, servicios internos para notificaciones y almacenamiento de imágenes.

## Requisitos previos

- .NET 9 SDK
- SQL Server (Express / Developer / LocalDB)
- Herramientas EF Core (`dotnet tool install --global dotnet-ef` si no lo tienes instalado)
- Node.js (opcional, solo si deseas recompilar assets Tailwind)

## Configuración rápida

1. **Clonar el repositorio**
   ```bash
   git clone https://github.com/bryanitesca/CitaFacil.git
   cd CitaFacil
   ```

2. **Configurar la cadena de conexión** en `CitaFacil/appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=CitaFacilDB;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true"
   }
   ```

3. **Aplicar migraciones y crear la base de datos**
   ```bash
   cd CitaFacil
   dotnet ef database update
   ```

4. **Ejecutar la aplicación**
   ```bash
   dotnet run
   ```
   Por defecto se expone en `https://localhost:5001` (o según tu `launchSettings.json`).

## Compilar desde el código fuente

1. **Restaurar dependencias**
   ```bash
   dotnet restore
   ```

2. **Compilar en modo Debug**
   ```bash
   dotnet build
   ```

3. **Compilar en modo Release**
   ```bash
   dotnet publish -c Release -o out
   ```
   El artefacto se genera en la carpeta `out/` listo para desplegarse (incluye archivos estáticos).

4. **Opcional**: recompilar estilos Tailwind (si se realizan cambios profundos en `wwwroot/css`)
   ```bash
   npm install
   npm run build:css
   ```

## Migraciones y datos de ejemplo

- Las migraciones se encuentran en `CitaFacil/Migrations`. Para crear nuevas migraciones:
  ```bash
  dotnet ef migrations add NombreDeMigracion
  dotnet ef database update
  ```
- El seeding inicial (`Data/Seed/DataSeeder.cs`) crea cuentas de muestra para cada rol junto con pacientes, doctores y citas.

## Respaldos y restauración

El módulo `Admin → Respaldos` permite:

- Configurar la carpeta de destino, horario y retención de archivos (`appsettings.json` provee valores por defecto a través de la sección `DatabaseBackup`).
- Generar respaldos manuales (servicio `DatabaseBackupHostedService`).  
- Restaurar la base de datos subiendo un archivo `.bak`. El proceso ejecuta comandos `BACKUP/RESTORE` reales sobre SQL Server; se recomienda ejecutarlo en horarios de mantenimiento por el modo de usuario único temporal.

Los archivos .bak se descargan en la carpeta "default" de descargas y se registra un historial de restauración `database_backup_history`.

## Estructura principal

```
CitaFacil/
├── Controllers/           Lógica MVC por área (pacientes, doctores, admin)
├── Data/                  DbContext, seeding y migraciones
├── Models/                Entidades EF Core
├── Services/              Servicios de dominio (usuarios, notificaciones, backups, etc.)
├── ViewModels/            Modelos fuertemente tipados para las vistas
├── Views/                 Vistas Razor organizadas por módulo
└── wwwroot/               Archivos estáticos (Tailwind, JS, imágenes)
```

## Notas

- Los roles y cuentas sembradas se encuentran en `Data/Seed/DataSeeder.cs`. Ajusta las contraseñas o crea usuarios manualmente desde el panel de administración.
- El flujo de agendado para pacientes reutiliza la experiencia de calendario del doctor, asegurando una selección intuitiva con validación de citas a +1 día.
- Las notificaciones se almacenan en la tabla `notificaciones` y se consumen internamente (sin SMTP).
