IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Especialidades] (
    [id] bigint NOT NULL IDENTITY,
    [nombre] nvarchar(100) NOT NULL,
    [descripcion] nvarchar(255) NULL,
    [icono] nvarchar(120) NULL,
    [activa] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_Especialidades] PRIMARY KEY ([id])
);

CREATE TABLE [Usuarios] (
    [id] bigint NOT NULL IDENTITY,
    [apellido] nvarchar(120) NULL,
    [nombre] nvarchar(100) NOT NULL,
    [usuario] nvarchar(60) NOT NULL,
    [contraseña] nvarchar(255) NOT NULL,
    [rol] nvarchar(50) NOT NULL,
    [correo] nvarchar(255) NOT NULL,
    [celular] nvarchar(20) NULL,
    [foto_url] nvarchar(255) NULL,
    [creado_el] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [activo] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_Usuarios] PRIMARY KEY ([id])
);

CREATE TABLE [Doctores] (
    [id] bigint NOT NULL IDENTITY,
    [usuario_id] bigint NOT NULL,
    [nombre] nvarchar(120) NOT NULL,
    [apellido] nvarchar(100) NOT NULL,
    [segundo_apellido] nvarchar(100) NULL,
    [licencia] nvarchar(100) NOT NULL,
    [telefono] nvarchar(25) NULL,
    [consultorio] nvarchar(255) NULL,
    [biografia] nvarchar(500) NULL,
    [especialidad_id] bigint NOT NULL,
    CONSTRAINT [PK_Doctores] PRIMARY KEY ([id]),
    CONSTRAINT [FK_Doctores_Especialidades_especialidad_id] FOREIGN KEY ([especialidad_id]) REFERENCES [Especialidades] ([id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Doctores_Usuarios_usuario_id] FOREIGN KEY ([usuario_id]) REFERENCES [Usuarios] ([id]) ON DELETE CASCADE
);

CREATE TABLE [Pacientes] (
    [id] bigint NOT NULL IDENTITY,
    [usuario_id] bigint NOT NULL,
    [nombre] nvarchar(120) NOT NULL,
    [apellido] nvarchar(100) NOT NULL,
    [segundo_apellido] nvarchar(100) NULL,
    [fecha_nacimiento] datetime2 NOT NULL,
    [genero] nvarchar(50) NULL,
    [direccion] nvarchar(255) NULL,
    CONSTRAINT [PK_Pacientes] PRIMARY KEY ([id]),
    CONSTRAINT [FK_Pacientes_Usuarios_usuario_id] FOREIGN KEY ([usuario_id]) REFERENCES [Usuarios] ([id]) ON DELETE CASCADE
);

CREATE TABLE [Citas] (
    [id] bigint NOT NULL IDENTITY,
    [fecha] datetime2 NOT NULL,
    [hora] time NOT NULL,
    [motivo] nvarchar(500) NULL,
    [notas] nvarchar(500) NULL,
    [ubicacion] nvarchar(120) NULL,
    [es_virtual] bit NOT NULL,
    [duracion_minutos] int NOT NULL DEFAULT 30,
    [estado] nvarchar(50) NOT NULL DEFAULT N'PENDIENTE',
    [paciente_id] bigint NOT NULL,
    [doctor_id] bigint NOT NULL,
    [creada_el] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [actualizada_el] datetime2 NULL,
    CONSTRAINT [PK_Citas] PRIMARY KEY ([id]),
    CONSTRAINT [FK_Citas_Doctores_doctor_id] FOREIGN KEY ([doctor_id]) REFERENCES [Doctores] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Citas_Pacientes_paciente_id] FOREIGN KEY ([paciente_id]) REFERENCES [Pacientes] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [notificaciones] (
    [id] bigint NOT NULL IDENTITY,
    [doctor_id] bigint NOT NULL,
    [paciente_id] bigint NOT NULL,
    [asunto] nvarchar(120) NOT NULL,
    [mensaje] nvarchar(1000) NOT NULL,
    [via_sistema] bit NOT NULL,
    [leida] bit NOT NULL,
    [enviada_el] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    CONSTRAINT [PK_notificaciones] PRIMARY KEY ([id]),
    CONSTRAINT [FK_notificaciones_Doctores_doctor_id] FOREIGN KEY ([doctor_id]) REFERENCES [Doctores] ([id]),
    CONSTRAINT [FK_notificaciones_Pacientes_paciente_id] FOREIGN KEY ([paciente_id]) REFERENCES [Pacientes] ([id])
);

CREATE INDEX [IX_Citas_doctor_id] ON [Citas] ([doctor_id]);

CREATE INDEX [IX_Citas_paciente_id] ON [Citas] ([paciente_id]);

CREATE INDEX [IX_Doctores_especialidad_id] ON [Doctores] ([especialidad_id]);

CREATE UNIQUE INDEX [IX_Doctores_licencia] ON [Doctores] ([licencia]);

CREATE UNIQUE INDEX [IX_Doctores_usuario_id] ON [Doctores] ([usuario_id]);

CREATE INDEX [IX_notificaciones_doctor_id] ON [notificaciones] ([doctor_id]);

CREATE INDEX [IX_notificaciones_paciente_id] ON [notificaciones] ([paciente_id]);

CREATE UNIQUE INDEX [IX_Pacientes_usuario_id] ON [Pacientes] ([usuario_id]);

CREATE UNIQUE INDEX [IX_Usuarios_correo] ON [Usuarios] ([correo]);

CREATE UNIQUE INDEX [IX_Usuarios_usuario] ON [Usuarios] ([usuario]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251105230337_InitialCreate', N'9.0.10');

DECLARE @var sysname;
SELECT @var = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Doctores]') AND [c].[name] = N'telefono');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Doctores] DROP CONSTRAINT [' + @var + '];');
ALTER TABLE [Doctores] DROP COLUMN [telefono];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251117213046_RemoveDoctorTelefono', N'9.0.10');

ALTER TABLE [Citas] ADD [motivo_cancelacion] nvarchar(500) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251117224940_AddMotivoCancelacionToCitas', N'9.0.10');

ALTER TABLE [Citas] ADD [diagnostico] nvarchar(500) NULL;

ALTER TABLE [Citas] ADD [tratamiento_recomendado] nvarchar(500) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251117230845_AddCamposConsulta', N'9.0.10');

DELETE FROM [Citas];

IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE NAME = 'id' AND OBJECT_ID = OBJECT_ID('[Citas]')) DBCC CHECKIDENT ('[Citas]', RESEED, 0);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251117234446_ClearCitasTable', N'9.0.10');

ALTER TABLE [Usuarios] ADD [segundo_apellido] nvarchar(120) NULL;


                UPDATE U
                SET segundo_apellido = COALESCE(P.segundo_apellido, D.segundo_apellido)
                FROM Usuarios U
                LEFT JOIN Pacientes P ON U.id = P.usuario_id
                LEFT JOIN Doctores D ON U.id = D.usuario_id
                WHERE U.segundo_apellido IS NULL
                  AND (P.segundo_apellido IS NOT NULL OR D.segundo_apellido IS NOT NULL);
            

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251118001400_AddSegundoApellidoToUsuarios', N'9.0.10');

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Citas]') AND [c].[name] = N'ubicacion');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Citas] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Citas] DROP COLUMN [ubicacion];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251125211118_RemoveCitaUbicacion', N'9.0.10');

CREATE TABLE [database_backup_configs] (
    [id] int NOT NULL IDENTITY,
    [backup_directory] nvarchar(260) NOT NULL,
    [auto_backup_enabled] bit NOT NULL,
    [auto_backup_time] time NOT NULL,
    [retention_days] int NOT NULL DEFAULT 30,
    [last_backup_utc] datetime2 NULL,
    [last_automatic_backup_utc] datetime2 NULL,
    CONSTRAINT [PK_database_backup_configs] PRIMARY KEY ([id])
);

CREATE TABLE [database_backup_history] (
    [id] bigint NOT NULL IDENTITY,
    [operation_type] nvarchar(80) NOT NULL,
    [status] nvarchar(120) NOT NULL,
    [file_path] nvarchar(260) NOT NULL,
    [file_name] nvarchar(160) NOT NULL,
    [created_utc] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [message] nvarchar(500) NULL,
    CONSTRAINT [PK_database_backup_history] PRIMARY KEY ([id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251125211658_CreateDatabaseBackupTables', N'9.0.10');

COMMIT;
GO

