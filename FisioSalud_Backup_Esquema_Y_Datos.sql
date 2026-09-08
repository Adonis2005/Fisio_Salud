-- ==========================================================
-- FISIOSALUD - SCRIPT COMPLETO DE BASE DE DATOS SQL SERVER
-- Fecha de generaciÃ³n: 2026-09-06 21:41:38
-- ==========================================================
USE [master];
GO


-- ----------------------------------------------------------
-- Tabla: [Roles]
-- ----------------------------------------------------------
IF OBJECT_ID('dbo.[Roles]', 'U') IS NOT NULL DROP TABLE dbo.[Roles];
GO
CREATE TABLE dbo.[Roles] (
    [RolId] int IDENTITY(1,1) NOT NULL,
    [Nombre] varchar(30) NOT NULL,
    [Descripcion] varchar(150) NULL,
    [Estado] bit NOT NULL DEFAULT ((1)),
    CONSTRAINT [PK_Roles] PRIMARY KEY ([RolId])
);
GO

-- Datos de [Roles]
SET IDENTITY_INSERT dbo.[Roles] ON;
INSERT INTO dbo.[Roles] ([RolId], [Nombre], [Descripcion], [Estado]) VALUES (1, N'Administrador', N'Administrador del sistema', 1);
INSERT INTO dbo.[Roles] ([RolId], [Nombre], [Descripcion], [Estado]) VALUES (2, N'Fisioterapeuta', N'Profesional de fisioterapia', 1);
INSERT INTO dbo.[Roles] ([RolId], [Nombre], [Descripcion], [Estado]) VALUES (3, N'Cliente', N'Cliente / Paciente', 1);
SET IDENTITY_INSERT dbo.[Roles] OFF;
GO

-- ----------------------------------------------------------
-- Tabla: [Usuarios]
-- ----------------------------------------------------------
IF OBJECT_ID('dbo.[Usuarios]', 'U') IS NOT NULL DROP TABLE dbo.[Usuarios];
GO
CREATE TABLE dbo.[Usuarios] (
    [UsuarioId] int IDENTITY(1,1) NOT NULL,
    [RolId] int NOT NULL,
    [Nombres] varchar(80) NOT NULL,
    [Apellidos] varchar(80) NOT NULL,
    [Identificacion] varchar(20) NOT NULL,
    [Correo] varchar(120) NOT NULL,
    [PasswordHash] varchar(255) NOT NULL,
    [Telefono] varchar(20) NULL,
    [Estado] bit NOT NULL DEFAULT ((1)),
    [FechaCreacion] datetime2 NOT NULL DEFAULT (sysdatetime()),
    [FechaActualizacion] datetime2 NULL,
    [UltimoAcceso] datetime2 NULL,
    CONSTRAINT [PK_Usuarios] PRIMARY KEY ([UsuarioId])
);
GO

-- Datos de [Usuarios]
SET IDENTITY_INSERT dbo.[Usuarios] ON;
INSERT INTO dbo.[Usuarios] ([UsuarioId], [RolId], [Nombres], [Apellidos], [Identificacion], [Correo], [PasswordHash], [Telefono], [Estado], [FechaCreacion], [FechaActualizacion], [UltimoAcceso]) VALUES (1, 1, N'Administrador', N'Sistema', N'0000000000', N'admin@fisiosalud.com', N'$2a$11$EfDAfHDiJKaoq644SJfM8etWVxcaEkqHd9uvi8guPm36UPhopvZ3i', N'0000000000', 1, '2026-08-03 20:11:01.000', '2026-09-06 18:23:39.000', '2026-09-06 20:53:47.000');
INSERT INTO dbo.[Usuarios] ([UsuarioId], [RolId], [Nombres], [Apellidos], [Identificacion], [Correo], [PasswordHash], [Telefono], [Estado], [FechaCreacion], [FechaActualizacion], [UltimoAcceso]) VALUES (2, 2, N'Juan', N'Perez', N'0000000002', N'fisio@fisiosalud.com', N'$2a$11$EfDAfHDiJKaoq644SJfM8etWVxcaEkqHd9uvi8guPm36UPhopvZ3i', N'1111111111', 1, '2026-08-04 22:30:05.000', '2026-09-06 18:23:39.000', '2026-09-06 20:58:01.000');
INSERT INTO dbo.[Usuarios] ([UsuarioId], [RolId], [Nombres], [Apellidos], [Identificacion], [Correo], [PasswordHash], [Telefono], [Estado], [FechaCreacion], [FechaActualizacion], [UltimoAcceso]) VALUES (3, 3, N'Maria', N'Gomez', N'0000000003', N'cliente@ejemplo.com', N'$2a$11$s4KCS1ZbrFcOMdXFUndCa.Y6PrVP3BLyNFpyjfoESSYLZjx65.Nsm', N'2222222222', 1, '2026-08-04 22:30:05.000', NULL, '2026-08-04 23:01:34.000');
INSERT INTO dbo.[Usuarios] ([UsuarioId], [RolId], [Nombres], [Apellidos], [Identificacion], [Correo], [PasswordHash], [Telefono], [Estado], [FechaCreacion], [FechaActualizacion], [UltimoAcceso]) VALUES (4, 3, N'Adonis', N'Espinoza', N'1729053437', N'elkin2005espinoza@gmail.com', N'$2a$11$h8K00uBgJe0YCtBh4It17..tfgD5J6rwFDz1FUWb/QPLKvatTepJa', N'+593 99 509 0472', 1, '2026-08-04 22:57:49.000', NULL, '2026-09-06 20:56:27.000');
SET IDENTITY_INSERT dbo.[Usuarios] OFF;
GO

-- ----------------------------------------------------------
-- Tabla: [Mensajes]
-- ----------------------------------------------------------
IF OBJECT_ID('dbo.[Mensajes]', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Mensajes] (
        [MensajeId] int IDENTITY(1,1) NOT NULL,
        [RemitenteId] int NOT NULL,
        [DestinatarioId] int NOT NULL,
        [Contenido] nvarchar(2000) NOT NULL,
        [FechaEnvio] datetime2 NOT NULL DEFAULT (sysdatetime()),
        [Leido] bit NOT NULL,
        CONSTRAINT [PK_Mensajes] PRIMARY KEY ([MensajeId])
    );
END;
GO

IF OBJECT_ID('dbo.[Mensajes]', 'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.[Mensajes] ALTER COLUMN [Contenido] nvarchar(2000) NOT NULL;
    ALTER TABLE dbo.[Mensajes] ALTER COLUMN [FechaEnvio] datetime2 NOT NULL;
    ALTER TABLE dbo.[Mensajes] ALTER COLUMN [Leido] bit NOT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Mensajes_RemitenteId' AND object_id = OBJECT_ID('dbo.[Mensajes]'))
    CREATE INDEX [IX_Mensajes_RemitenteId] ON dbo.[Mensajes] ([RemitenteId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Mensajes_DestinatarioId' AND object_id = OBJECT_ID('dbo.[Mensajes]'))
    CREATE INDEX [IX_Mensajes_DestinatarioId] ON dbo.[Mensajes] ([DestinatarioId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Mensajes_Conversacion_FechaEnvio' AND object_id = OBJECT_ID('dbo.[Mensajes]'))
    CREATE INDEX [IX_Mensajes_Conversacion_FechaEnvio]
        ON dbo.[Mensajes] ([RemitenteId], [DestinatarioId], [FechaEnvio]);
GO

-- ----------------------------------------------------------
-- Tabla: [Pacientes]
-- ----------------------------------------------------------
IF OBJECT_ID('dbo.[Pacientes]', 'U') IS NOT NULL DROP TABLE dbo.[Pacientes];
GO
CREATE TABLE dbo.[Pacientes] (
    [PacienteId] int IDENTITY(1,1) NOT NULL,
    [Nombres] varchar(80) NOT NULL,
    [Apellidos] varchar(80) NOT NULL,
    [Identificacion] varchar(20) NOT NULL,
    [FechaNacimiento] date NOT NULL,
    [Sexo] char(1) NOT NULL,
    [Direccion] varchar(200) NULL,
    [Telefono] varchar(20) NULL,
    [Correo] varchar(120) NULL,
    [Estado] bit NOT NULL DEFAULT ((1)),
    [FechaRegistro] datetime2 NOT NULL DEFAULT (sysdatetime()),
    [FechaActualizacion] datetime2 NULL,
    [UsuarioId] int NULL,
    CONSTRAINT [PK_Pacientes] PRIMARY KEY ([PacienteId])
);
GO

-- Datos de [Pacientes]
SET IDENTITY_INSERT dbo.[Pacientes] ON;
INSERT INTO dbo.[Pacientes] ([PacienteId], [Nombres], [Apellidos], [Identificacion], [FechaNacimiento], [Sexo], [Direccion], [Telefono], [Correo], [Estado], [FechaRegistro], [FechaActualizacion], [UsuarioId]) VALUES (1, N'Adonis', N'Espinoza', N'1729053437', '2008-08-04 00:00:00.000', N'O', NULL, N'+593 99 509 0472', N'elkin2005espinoza@gmail.com', 1, '2026-08-04 22:57:49.000', NULL, 4);
SET IDENTITY_INSERT dbo.[Pacientes] OFF;
GO

-- ----------------------------------------------------------
-- Tabla: [Patologias]
-- ----------------------------------------------------------
IF OBJECT_ID('dbo.[Patologias]', 'U') IS NOT NULL DROP TABLE dbo.[Patologias];
GO
CREATE TABLE dbo.[Patologias] (
    [PatologiaId] int IDENTITY(1,1) NOT NULL,
    [Nombre] varchar(120) NOT NULL,
    [Descripcion] varchar(500) NULL,
    [Estado] bit NOT NULL DEFAULT ((1)),
    [FechaRegistro] datetime2 NOT NULL DEFAULT (sysdatetime()),
    CONSTRAINT [PK_Patologias] PRIMARY KEY ([PatologiaId])
);
GO

-- ----------------------------------------------------------
-- Tabla: [Diagnosticos]
-- ----------------------------------------------------------
IF OBJECT_ID('dbo.[Diagnosticos]', 'U') IS NOT NULL DROP TABLE dbo.[Diagnosticos];
GO
CREATE TABLE dbo.[Diagnosticos] (
    [DiagnosticoId] int IDENTITY(1,1) NOT NULL,
    [PacienteId] int NOT NULL,
    [PatologiaId] int NULL,
    [FisioterapeutaId] int NOT NULL,
    [Diagnostico] varchar(500) NOT NULL,
    [Observaciones] varchar(1000) NULL,
    [EvaluacionFuncional] varchar(1000) NULL,
    [FechaDiagnostico] date NOT NULL DEFAULT (CONVERT([date],getdate())),
    [Estado] bit NOT NULL DEFAULT ((1)),
    [FechaRegistro] datetime2 NOT NULL DEFAULT (sysdatetime()),
    CONSTRAINT [PK_Diagnosticos] PRIMARY KEY ([DiagnosticoId])
);
GO

-- ----------------------------------------------------------
-- Tabla: [PlanesTratamiento]
-- ----------------------------------------------------------
IF OBJECT_ID('dbo.[PlanesTratamiento]', 'U') IS NOT NULL DROP TABLE dbo.[PlanesTratamiento];
GO
CREATE TABLE dbo.[PlanesTratamiento] (
    [PlanTratamientoId] int IDENTITY(1,1) NOT NULL,
    [PacienteId] int NOT NULL,
    [DiagnosticoId] int NOT NULL,
    [FisioterapeutaId] int NOT NULL,
    [Nombre] varchar(150) NOT NULL,
    [Objetivos] varchar(1000) NOT NULL,
    [DuracionEstimada] int NULL,
    [FechaInicio] date NOT NULL,
    [FechaFinEstimada] date NULL,
    [Estado] varchar(20) NOT NULL DEFAULT ('ACTIVO'),
    [Observaciones] varchar(1000) NULL,
    [FechaCreacion] datetime2 NOT NULL DEFAULT (sysdatetime()),
    [FechaActualizacion] datetime2 NULL,
    CONSTRAINT [PK_PlanesTratamiento] PRIMARY KEY ([PlanTratamientoId])
);
GO

-- ----------------------------------------------------------
-- Tabla: [Ejercicios]
-- ----------------------------------------------------------
IF OBJECT_ID('dbo.[Ejercicios]', 'U') IS NOT NULL DROP TABLE dbo.[Ejercicios];
GO
CREATE TABLE dbo.[Ejercicios] (
    [EjercicioId] int IDENTITY(1,1) NOT NULL,
    [Nombre] varchar(150) NOT NULL,
    [Descripcion] varchar(1000) NULL,
    [DuracionMinutos] int NULL,
    [Recomendaciones] varchar(1000) NULL,
    [Estado] bit NOT NULL DEFAULT ((1)),
    [FechaRegistro] datetime2 NOT NULL DEFAULT (sysdatetime()),
    CONSTRAINT [PK_Ejercicios] PRIMARY KEY ([EjercicioId])
);
GO

-- ----------------------------------------------------------
-- Tabla: [TratamientoEjercicios]
-- ----------------------------------------------------------
IF OBJECT_ID('dbo.[TratamientoEjercicios]', 'U') IS NOT NULL DROP TABLE dbo.[TratamientoEjercicios];
GO
CREATE TABLE dbo.[TratamientoEjercicios] (
    [TratamientoEjercicioId] int IDENTITY(1,1) NOT NULL,
    [PlanTratamientoId] int NOT NULL,
    [EjercicioId] int NOT NULL,
    [Frecuencia] varchar(100) NOT NULL,
    [Series] int NULL,
    [Repeticiones] int NULL,
    [Observaciones] varchar(1000) NULL,
    [Estado] bit NOT NULL DEFAULT ((1)),
    [FechaAsignacion] datetime2 NOT NULL DEFAULT (sysdatetime()),
    CONSTRAINT [PK_TratamientoEjercicios] PRIMARY KEY ([TratamientoEjercicioId])
);
GO

-- ----------------------------------------------------------
-- Tabla: [Citas]
-- ----------------------------------------------------------
IF OBJECT_ID('dbo.[Citas]', 'U') IS NOT NULL DROP TABLE dbo.[Citas];
GO
CREATE TABLE dbo.[Citas] (
    [CitaId] int IDENTITY(1,1) NOT NULL,
    [PacienteId] int NOT NULL,
    [FisioterapeutaId] int NOT NULL,
    [Fecha] date NOT NULL,
    [HoraInicio] time NOT NULL,
    [HoraFin] time NULL,
    [MotivoConsulta] varchar(500) NULL,
    [Estado] varchar(20) NOT NULL DEFAULT ('PROGRAMADA'),
    [Observaciones] varchar(1000) NULL,
    [FechaRegistro] datetime2 NOT NULL DEFAULT (sysdatetime()),
    [FechaActualizacion] datetime2 NULL,
    CONSTRAINT [PK_Citas] PRIMARY KEY ([CitaId])
);
GO

-- ----------------------------------------------------------
-- Tabla: [SesionesRehabilitacion]
-- ----------------------------------------------------------
IF OBJECT_ID('dbo.[SesionesRehabilitacion]', 'U') IS NOT NULL DROP TABLE dbo.[SesionesRehabilitacion];
GO
CREATE TABLE dbo.[SesionesRehabilitacion] (
    [SesionId] int IDENTITY(1,1) NOT NULL,
    [PlanTratamientoId] int NOT NULL,
    [CitaId] int NULL,
    [FisioterapeutaId] int NOT NULL,
    [NumeroSesion] int NOT NULL,
    [FechaSesion] date NOT NULL,
    [ActividadesRealizadas] varchar(1500) NOT NULL,
    [Observaciones] varchar(1500) NULL,
    [Resultados] varchar(1500) NULL,
    [Estado] varchar(20) NOT NULL DEFAULT ('REALIZADA'),
    [FechaRegistro] datetime2 NOT NULL DEFAULT (sysdatetime()),
    CONSTRAINT [PK_SesionesRehabilitacion] PRIMARY KEY ([SesionId])
);
GO

-- ----------------------------------------------------------
-- Tabla: [Seguimientos]
-- ----------------------------------------------------------
IF OBJECT_ID('dbo.[Seguimientos]', 'U') IS NOT NULL DROP TABLE dbo.[Seguimientos];
GO
CREATE TABLE dbo.[Seguimientos] (
    [SeguimientoId] int IDENTITY(1,1) NOT NULL,
    [SesionId] int NOT NULL,
    [NivelDolor] decimal(18,2) NULL,
    [MovilidadArticular] decimal(18,2) NULL,
    [FuerzaMuscular] decimal(18,2) NULL,
    [GradoRecuperacion] decimal(18,2) NULL,
    [Observaciones] varchar(1500) NULL,
    [FechaRegistro] datetime2 NOT NULL DEFAULT (sysdatetime()),
    [TratamientoEjercicioId] int NULL,
    CONSTRAINT [PK_Seguimientos] PRIMARY KEY ([SeguimientoId])
);
GO

-- ----------------------------------------------------------
-- Tabla: [PasswordResetTokens]
-- ----------------------------------------------------------
IF OBJECT_ID('dbo.[PasswordResetTokens]', 'U') IS NOT NULL DROP TABLE dbo.[PasswordResetTokens];
GO
CREATE TABLE dbo.[PasswordResetTokens] (
    [TokenId] int IDENTITY(1,1) NOT NULL,
    [UsuarioId] int NOT NULL,
    [Token] varchar(255) NOT NULL,
    [FechaExpiracion] datetime2 NOT NULL,
    [Usado] bit NOT NULL DEFAULT ((0)),
    [FechaCreacion] datetime2 NOT NULL DEFAULT (sysdatetime()),
    CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY ([TokenId])
);
GO

-- ----------------------------------------------------------
-- Tabla: [SesionesUsuario]
-- ----------------------------------------------------------
IF OBJECT_ID('dbo.[SesionesUsuario]', 'U') IS NOT NULL DROP TABLE dbo.[SesionesUsuario];
GO
CREATE TABLE dbo.[SesionesUsuario] (
    [SesionUsuarioId] bigint IDENTITY(1,1) NOT NULL,
    [UsuarioId] int NOT NULL,
    [TokenSesion] varchar(255) NOT NULL,
    [FechaInicio] datetime2 NOT NULL DEFAULT (sysdatetime()),
    [FechaUltimaActividad] datetime2 NOT NULL DEFAULT (sysdatetime()),
    [FechaCierre] datetime2 NULL,
    [Activa] bit NOT NULL DEFAULT ((1)),
    [DireccionIP] varchar(45) NULL,
    CONSTRAINT [PK_SesionesUsuario] PRIMARY KEY ([SesionUsuarioId])
);
GO

-- ----------------------------------------------------------
-- Tabla: [Auditoria]
-- ----------------------------------------------------------
IF OBJECT_ID('dbo.[Auditoria]', 'U') IS NOT NULL DROP TABLE dbo.[Auditoria];
GO
CREATE TABLE dbo.[Auditoria] (
    [AuditoriaId] bigint IDENTITY(1,1) NOT NULL,
    [UsuarioId] int NULL,
    [TablaAfectada] varchar(100) NOT NULL,
    [Accion] varchar(20) NOT NULL,
    [RegistroId] varchar(50) NULL,
    [FechaHora] datetime2 NOT NULL DEFAULT (sysdatetime()),
    [DireccionIP] varchar(45) NULL,
    [Descripcion] varchar(1000) NULL,
    CONSTRAINT [PK_Auditoria] PRIMARY KEY ([AuditoriaId])
);
GO

-- ----------------------------------------------------------
-- Tabla: [Facturas]
-- ----------------------------------------------------------
IF OBJECT_ID('dbo.[Facturas]', 'U') IS NOT NULL DROP TABLE dbo.[Facturas];
GO
CREATE TABLE dbo.[Facturas] (
    [FacturaId] int IDENTITY(1,1) NOT NULL,
    [NumeroFactura] varchar(50) NOT NULL,
    [PacienteId] int NOT NULL,
    [FisioterapeutaId] int NULL,
    [Monto] decimal(18,2) NOT NULL,
    [Fecha] date NOT NULL,
    [Estado] varchar(20) NOT NULL,
    [FechaRegistro] datetime2 NOT NULL DEFAULT (sysdatetime()),
    [UsuarioId] int NULL,
    CONSTRAINT [PK_Facturas] PRIMARY KEY ([FacturaId])
);
GO

-- ----------------------------------------------------------
-- Tabla: [DetallesFactura]
-- ----------------------------------------------------------
IF OBJECT_ID('dbo.[DetallesFactura]', 'U') IS NOT NULL DROP TABLE dbo.[DetallesFactura];
GO
CREATE TABLE dbo.[DetallesFactura] (
    [DetalleFacturaId] int IDENTITY(1,1) NOT NULL,
    [FacturaId] int NOT NULL,
    [CitaId] int NULL,
    [Concepto] varchar(250) NOT NULL,
    [Cantidad] int NOT NULL DEFAULT ((1)),
    [PrecioUnitario] decimal(18,2) NOT NULL,
    [Subtotal] decimal(18,2) NOT NULL,
    [FechaRegistro] datetime2 NOT NULL DEFAULT (sysdatetime()),
    CONSTRAINT [PK_DetallesFactura] PRIMARY KEY ([DetalleFacturaId])
);
GO

-- ==========================================================
-- LLAVES FORÃNEAS
-- ==========================================================
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Auditoria_Usuarios')
    ALTER TABLE dbo.[Auditoria] ADD CONSTRAINT [FK_Auditoria_Usuarios] FOREIGN KEY ([UsuarioId]) REFERENCES dbo.[Usuarios]([UsuarioId]);
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Citas_Fisioterapeuta')
    ALTER TABLE dbo.[Citas] ADD CONSTRAINT [FK_Citas_Fisioterapeuta] FOREIGN KEY ([FisioterapeutaId]) REFERENCES dbo.[Usuarios]([UsuarioId]);
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Citas_Pacientes')
    ALTER TABLE dbo.[Citas] ADD CONSTRAINT [FK_Citas_Pacientes] FOREIGN KEY ([PacienteId]) REFERENCES dbo.[Pacientes]([PacienteId]);
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DetallesFactura_Facturas')
    ALTER TABLE dbo.[DetallesFactura] ADD CONSTRAINT [FK_DetallesFactura_Facturas] FOREIGN KEY ([FacturaId]) REFERENCES dbo.[Facturas]([FacturaId]);
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DetallesFactura_Citas')
    ALTER TABLE dbo.[DetallesFactura] ADD CONSTRAINT [FK_DetallesFactura_Citas] FOREIGN KEY ([CitaId]) REFERENCES dbo.[Citas]([CitaId]);
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Diagnosticos_Pacientes')
    ALTER TABLE dbo.[Diagnosticos] ADD CONSTRAINT [FK_Diagnosticos_Pacientes] FOREIGN KEY ([PacienteId]) REFERENCES dbo.[Pacientes]([PacienteId]);
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Diagnosticos_Patologias')
    ALTER TABLE dbo.[Diagnosticos] ADD CONSTRAINT [FK_Diagnosticos_Patologias] FOREIGN KEY ([PatologiaId]) REFERENCES dbo.[Patologias]([PatologiaId]);
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Diagnosticos_Fisioterapeuta')
    ALTER TABLE dbo.[Diagnosticos] ADD CONSTRAINT [FK_Diagnosticos_Fisioterapeuta] FOREIGN KEY ([FisioterapeutaId]) REFERENCES dbo.[Usuarios]([UsuarioId]);
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Facturas_Fisioterapeutas')
    ALTER TABLE dbo.[Facturas] ADD CONSTRAINT [FK_Facturas_Fisioterapeutas] FOREIGN KEY ([FisioterapeutaId]) REFERENCES dbo.[Usuarios]([UsuarioId]);
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Facturas_Usuarios')
    ALTER TABLE dbo.[Facturas] ADD CONSTRAINT [FK_Facturas_Usuarios] FOREIGN KEY ([UsuarioId]) REFERENCES dbo.[Usuarios]([UsuarioId]);
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Facturas_Pacientes')
    ALTER TABLE dbo.[Facturas] ADD CONSTRAINT [FK_Facturas_Pacientes] FOREIGN KEY ([PacienteId]) REFERENCES dbo.[Pacientes]([PacienteId]);
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Pacientes_Usuarios')
    ALTER TABLE dbo.[Pacientes] ADD CONSTRAINT [FK_Pacientes_Usuarios] FOREIGN KEY ([UsuarioId]) REFERENCES dbo.[Usuarios]([UsuarioId]);
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Mensajes_Remitente')
    ALTER TABLE dbo.[Mensajes] ADD CONSTRAINT [FK_Mensajes_Remitente] FOREIGN KEY ([RemitenteId]) REFERENCES dbo.[Usuarios]([UsuarioId]);
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Mensajes_Destinatario')
    ALTER TABLE dbo.[Mensajes] ADD CONSTRAINT [FK_Mensajes_Destinatario] FOREIGN KEY ([DestinatarioId]) REFERENCES dbo.[Usuarios]([UsuarioId]);
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PasswordResetTokens_Usuarios')
    ALTER TABLE dbo.[PasswordResetTokens] ADD CONSTRAINT [FK_PasswordResetTokens_Usuarios] FOREIGN KEY ([UsuarioId]) REFERENCES dbo.[Usuarios]([UsuarioId]);
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PlanesTratamiento_Fisioterapeuta')
    ALTER TABLE dbo.[PlanesTratamiento] ADD CONSTRAINT [FK_PlanesTratamiento_Fisioterapeuta] FOREIGN KEY ([FisioterapeutaId]) REFERENCES dbo.[Usuarios]([UsuarioId]);
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PlanesTratamiento_Diagnosticos')
    ALTER TABLE dbo.[PlanesTratamiento] ADD CONSTRAINT [FK_PlanesTratamiento_Diagnosticos] FOREIGN KEY ([DiagnosticoId]) REFERENCES dbo.[Diagnosticos]([DiagnosticoId]);
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PlanesTratamiento_Pacientes')
    ALTER TABLE dbo.[PlanesTratamiento] ADD CONSTRAINT [FK_PlanesTratamiento_Pacientes] FOREIGN KEY ([PacienteId]) REFERENCES dbo.[Pacientes]([PacienteId]);
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Seguimientos_TratamientoEjercicios')
    ALTER TABLE dbo.[Seguimientos] ADD CONSTRAINT [FK_Seguimientos_TratamientoEjercicios] FOREIGN KEY ([TratamientoEjercicioId]) REFERENCES dbo.[TratamientoEjercicios]([TratamientoEjercicioId]);
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Seguimientos_Sesiones')
    ALTER TABLE dbo.[Seguimientos] ADD CONSTRAINT [FK_Seguimientos_Sesiones] FOREIGN KEY ([SesionId]) REFERENCES dbo.[SesionesRehabilitacion]([SesionId]);
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Sesiones_Cita')
    ALTER TABLE dbo.[SesionesRehabilitacion] ADD CONSTRAINT [FK_Sesiones_Cita] FOREIGN KEY ([CitaId]) REFERENCES dbo.[Citas]([CitaId]);
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Sesiones_Plan')
    ALTER TABLE dbo.[SesionesRehabilitacion] ADD CONSTRAINT [FK_Sesiones_Plan] FOREIGN KEY ([PlanTratamientoId]) REFERENCES dbo.[PlanesTratamiento]([PlanTratamientoId]);
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Sesiones_Fisioterapeuta')
    ALTER TABLE dbo.[SesionesRehabilitacion] ADD CONSTRAINT [FK_Sesiones_Fisioterapeuta] FOREIGN KEY ([FisioterapeutaId]) REFERENCES dbo.[Usuarios]([UsuarioId]);
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_SesionesUsuario_Usuarios')
    ALTER TABLE dbo.[SesionesUsuario] ADD CONSTRAINT [FK_SesionesUsuario_Usuarios] FOREIGN KEY ([UsuarioId]) REFERENCES dbo.[Usuarios]([UsuarioId]);
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_TratamientoEjercicios_Plan')
    ALTER TABLE dbo.[TratamientoEjercicios] ADD CONSTRAINT [FK_TratamientoEjercicios_Plan] FOREIGN KEY ([PlanTratamientoId]) REFERENCES dbo.[PlanesTratamiento]([PlanTratamientoId]);
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_TratamientoEjercicios_Ejercicio')
    ALTER TABLE dbo.[TratamientoEjercicios] ADD CONSTRAINT [FK_TratamientoEjercicios_Ejercicio] FOREIGN KEY ([EjercicioId]) REFERENCES dbo.[Ejercicios]([EjercicioId]);
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Usuarios_Roles')
    ALTER TABLE dbo.[Usuarios] ADD CONSTRAINT [FK_Usuarios_Roles] FOREIGN KEY ([RolId]) REFERENCES dbo.[Roles]([RolId]);
GO

-- ==========================================================
-- AMPLIACIÓN DEL FLUJO CLÍNICO Y DE PAGOS
-- ==========================================================
IF OBJECT_ID('dbo.[EvaluacionesIniciales]', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.[EvaluacionesIniciales] ([EvaluacionInicialId] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_EvaluacionesIniciales] PRIMARY KEY, [PacienteId] int NOT NULL, [FisioterapeutaId] int NOT NULL, [CitaId] int NULL, [MotivoConsulta] varchar(1000) NOT NULL, [Antecedentes] varchar(2000) NULL, [DolorInicial] decimal(18,2) NULL, [EvaluacionFisica] varchar(2000) NULL, [Observaciones] varchar(2000) NULL, [FechaEvaluacion] datetime2 NOT NULL DEFAULT (sysdatetime()), [FechaRegistro] datetime2 NOT NULL DEFAULT (sysdatetime()));
END;
GO
IF OBJECT_ID('dbo.[Servicios]', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Servicios] ([ServicioId] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Servicios] PRIMARY KEY, [Nombre] varchar(150) NOT NULL, [Descripcion] varchar(500) NULL, [Precio] decimal(18,2) NOT NULL, [Tipo] varchar(20) NOT NULL, [Estado] bit NOT NULL DEFAULT ((1)), [FechaRegistro] datetime2 NOT NULL DEFAULT (sysdatetime()));
END;
GO
IF OBJECT_ID('dbo.[Pagos]', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Pagos] ([PagoId] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Pagos] PRIMARY KEY, [FacturaId] int NOT NULL, [MetodoPago] varchar(30) NOT NULL, [Monto] decimal(18,2) NOT NULL, [Estado] varchar(20) NOT NULL DEFAULT ('PENDIENTE'), [Referencia] varchar(100) NULL, [FechaPago] datetime2 NULL, [FechaRegistro] datetime2 NOT NULL DEFAULT (sysdatetime()));
END;
GO
IF OBJECT_ID('dbo.[TratamientosAplicados]', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.[TratamientosAplicados] ([TratamientoAplicadoId] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_TratamientosAplicados] PRIMARY KEY, [Nombre] varchar(150) NOT NULL, [Descripcion] varchar(500) NULL, [Estado] bit NOT NULL DEFAULT ((1)), [FechaRegistro] datetime2 NOT NULL DEFAULT (sysdatetime()));
END;
GO
IF OBJECT_ID('dbo.[SesionTratamientos]', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.[SesionTratamientos] ([SesionTratamientoId] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_SesionTratamientos] PRIMARY KEY, [SesionId] int NOT NULL, [TratamientoAplicadoId] int NOT NULL, [DuracionMinutos] int NULL, [Observaciones] varchar(1000) NULL, [Resultado] varchar(1000) NULL);
END;
GO
IF OBJECT_ID('dbo.[EjerciciosRealizados]', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.[EjerciciosRealizados] ([EjercicioRealizadoId] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_EjerciciosRealizados] PRIMARY KEY, [TratamientoEjercicioId] int NOT NULL, [SesionId] int NULL, [PacienteId] int NOT NULL, [FechaRealizacion] datetime2 NOT NULL DEFAULT (sysdatetime()), [SeriesRealizadas] int NULL, [RepeticionesRealizadas] int NULL, [Dolor] decimal(18,2) NULL, [Estado] varchar(20) NOT NULL DEFAULT ('REALIZADO'), [Comentarios] varchar(1000) NULL);
END;
GO
IF OBJECT_ID('dbo.[DisponibilidadesFisioterapeuta]', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.[DisponibilidadesFisioterapeuta] ([DisponibilidadId] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_DisponibilidadesFisioterapeuta] PRIMARY KEY, [FisioterapeutaId] int NOT NULL, [DiaSemana] tinyint NOT NULL, [HoraInicio] time NOT NULL, [HoraFin] time NOT NULL, [Estado] bit NOT NULL DEFAULT ((1)), CONSTRAINT [CK_Disponibilidad_DiaSemana] CHECK ([DiaSemana] BETWEEN 1 AND 7), CONSTRAINT [CK_Disponibilidad_Horas] CHECK ([HoraFin] > [HoraInicio]));
END;
GO
IF COL_LENGTH('dbo.DetallesFactura', 'ServicioId') IS NULL ALTER TABLE dbo.[DetallesFactura] ADD [ServicioId] int NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EvaluacionesIniciales_Pacientes') ALTER TABLE dbo.[EvaluacionesIniciales] ADD CONSTRAINT [FK_EvaluacionesIniciales_Pacientes] FOREIGN KEY ([PacienteId]) REFERENCES dbo.[Pacientes]([PacienteId]);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EvaluacionesIniciales_Fisioterapeutas') ALTER TABLE dbo.[EvaluacionesIniciales] ADD CONSTRAINT [FK_EvaluacionesIniciales_Fisioterapeutas] FOREIGN KEY ([FisioterapeutaId]) REFERENCES dbo.[Usuarios]([UsuarioId]);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EvaluacionesIniciales_Citas') ALTER TABLE dbo.[EvaluacionesIniciales] ADD CONSTRAINT [FK_EvaluacionesIniciales_Citas] FOREIGN KEY ([CitaId]) REFERENCES dbo.[Citas]([CitaId]);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Pagos_Facturas') ALTER TABLE dbo.[Pagos] ADD CONSTRAINT [FK_Pagos_Facturas] FOREIGN KEY ([FacturaId]) REFERENCES dbo.[Facturas]([FacturaId]);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SesionTratamientos_Sesiones') ALTER TABLE dbo.[SesionTratamientos] ADD CONSTRAINT [FK_SesionTratamientos_Sesiones] FOREIGN KEY ([SesionId]) REFERENCES dbo.[SesionesRehabilitacion]([SesionId]);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SesionTratamientos_Tratamientos') ALTER TABLE dbo.[SesionTratamientos] ADD CONSTRAINT [FK_SesionTratamientos_Tratamientos] FOREIGN KEY ([TratamientoAplicadoId]) REFERENCES dbo.[TratamientosAplicados]([TratamientoAplicadoId]);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EjerciciosRealizados_Asignaciones') ALTER TABLE dbo.[EjerciciosRealizados] ADD CONSTRAINT [FK_EjerciciosRealizados_Asignaciones] FOREIGN KEY ([TratamientoEjercicioId]) REFERENCES dbo.[TratamientoEjercicios]([TratamientoEjercicioId]);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EjerciciosRealizados_Sesiones') ALTER TABLE dbo.[EjerciciosRealizados] ADD CONSTRAINT [FK_EjerciciosRealizados_Sesiones] FOREIGN KEY ([SesionId]) REFERENCES dbo.[SesionesRehabilitacion]([SesionId]);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EjerciciosRealizados_Pacientes') ALTER TABLE dbo.[EjerciciosRealizados] ADD CONSTRAINT [FK_EjerciciosRealizados_Pacientes] FOREIGN KEY ([PacienteId]) REFERENCES dbo.[Pacientes]([PacienteId]);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Disponibilidades_Fisioterapeutas') ALTER TABLE dbo.[DisponibilidadesFisioterapeuta] ADD CONSTRAINT [FK_Disponibilidades_Fisioterapeutas] FOREIGN KEY ([FisioterapeutaId]) REFERENCES dbo.[Usuarios]([UsuarioId]);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DetallesFactura_Servicios') ALTER TABLE dbo.[DetallesFactura] ADD CONSTRAINT [FK_DetallesFactura_Servicios] FOREIGN KEY ([ServicioId]) REFERENCES dbo.[Servicios]([ServicioId]);
GO
