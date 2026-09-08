# FisioSalud

Sistema web de gestión para un centro de fisioterapia. El proyecto permite administrar usuarios por roles, pacientes, citas, evaluación inicial, planes de tratamiento, ejercicios, sesiones de rehabilitación, seguimiento, equipos terapéuticos, facturación/pagos, mensajería y configuración del sistema.

> **Estado del proyecto:** la versión revisada corresponde a la rama `v0/completar-modelo-fisiosalud` del repositorio `Adonis2005/Fisio_Salud`.

## 1. ¿Qué es FisioSalud?

FisioSalud es una aplicación web desarrollada con **ASP.NET Core MVC** y **C#**. Su objetivo es centralizar la gestión de un consultorio/centro de fisioterapia en una sola aplicación.

El flujo general del sistema es:

**Registro / inicio de sesión → asignación de rol → gestión de pacientes → agenda de citas → evaluación → diagnóstico → plan de tratamiento → sesiones y ejercicios → seguimiento → facturación y pagos.**

El acceso cambia según el rol del usuario:

| Rol | Función principal |
|---|---|
| Administrador | Administra usuarios, citas, facturas/pagos, servicios, disponibilidad, ejercicios, patologías, equipos y configuraciones. |
| Fisioterapeuta | Gestiona pacientes asignados, agenda, evaluación inicial, atención, planes de tratamiento, ejercicios y seguimiento clínico. |
| Cliente / Paciente | Consulta su información, perfil, citas, facturas/pagos y actividades indicadas por el profesional. |

La aplicación implementa autenticación mediante cookies y autorización basada en roles. La cookie de autenticación se denomina `FisioSalud.Auth` y la sesión tiene una duración configurada de 8 horas con renovación deslizante. 

## 2. Tecnologías utilizadas

- **C#**
- **ASP.NET Core MVC**
- **.NET 5.0**
- **Entity Framework Core 5.0.17**
- **SQL Server**
- **BCrypt.Net-Next 4.0.3** para el tratamiento de contraseñas
- **Razor Views (`.cshtml`)** para la interfaz
- **SMTP/Gmail** para envío de correos, principalmente recuperación de contraseña

El archivo `FisioSalud_Proyecto.csproj` fija el target del proyecto en `net5.0` y referencia Entity Framework Core para SQL Server y BCrypt. 

## 3. Estructura del proyecto

```text
Fisio_Salud/
├── FisioSalud_Proyecto.sln
├── FisioSalud_Actualizacion_NoDestructiva.sql
├── FisioSalud_Backup_Esquema_Y_Datos.sql
├── FisioSalud_Proyecto/
│   ├── Controllers/
│   │   ├── Administrador/
│   │   ├── Auth/
│   │   ├── Cliente/
│   │   ├── Fisioterapeuta/
│   │   ├── Home/
│   │   ├── AgendaGestionController.cs
│   │   ├── AjustesController.cs
│   │   ├── CobrosController.cs
│   │   └── PlanesController.cs
│   ├── Data/
│   │   ├── DbInitializer.cs
│   │   └── FisioSaludDbContext.cs
│   ├── Helpers/
│   ├── Models/
│   │   ├── Administrador/
│   │   ├── Auth/
│   │   ├── Cliente/
│   │   ├── Clinical/
│   │   ├── Contacto/
│   │   ├── Entities/
│   │   └── Fisioterapeuta/
│   ├── Services/
│   ├── Views/
│   ├── wwwroot/
│   ├── Program.cs
│   ├── Startup.cs
│   ├── appsettings.json
│   └── appsettings.Development.json
└── .vs/
```

### Responsabilidad de las carpetas principales

**Controllers:** reciben las solicitudes del usuario, validan acceso y llaman a los servicios.

**Services:** concentran la lógica de negocio. El proyecto incluye servicios para autenticación, usuarios, panel administrativo, pacientes, citas, equipos, mensajes, correo y contraseñas.

**Models:** contiene entidades de base de datos y modelos para formularios/vistas.

**Data:** configura Entity Framework Core y el inicializador de base de datos.

**Views:** contiene las pantallas Razor agrupadas por módulo.

**wwwroot:** contiene recursos estáticos del sitio (CSS, JavaScript, imágenes, etc.).

## 4. Requisitos para instalarlo en otra computadora

La computadora donde se vaya a ejecutar necesita, como mínimo:

1. **Windows** (recomendado para esta versión del proyecto).
2. **SDK de .NET 5.0**, porque el proyecto compila para `net5.0`.
3. **SQL Server** (la configuración original usa un servidor SQL Server/SQL Express).
4. **SQL Server Management Studio (SSMS)** para restaurar o ejecutar los scripts SQL con facilidad.
5. **Visual Studio** con soporte para desarrollo de ASP.NET Core/C# si se desea abrir y ejecutar desde el IDE.
6. Acceso a Internet si se necesita restaurar paquetes NuGet por primera vez o utilizar el envío de correo.

> **Nota:** .NET 5 es una versión antigua del framework. Para reproducir este proyecto tal como está, se debe conservar el target `net5.0`; migrarlo a una versión moderna de .NET es un trabajo de actualización separado.

## 5. Cómo descargar el proyecto

Abrir una terminal y ejecutar:

```bash
git clone https://github.com/Adonis2005/Fisio_Salud.git
cd Fisio_Salud
```

Después cambiar a la rama de trabajo revisada:

```bash
git checkout v0/completar-modelo-fisiosalud
```

También es posible descargar el ZIP desde GitHub y trabajar directamente sobre los archivos.

## 6. Configurar la base de datos

### 6.1. Crear la base de datos

El repositorio incluye el archivo:

```text
FisioSalud_Backup_Esquema_Y_Datos.sql
```

Este archivo contiene el esquema y datos necesarios para levantar la base de datos de FisioSalud. La forma recomendada de preparar una instalación nueva es:

1. Abrir **SQL Server Management Studio**.
2. Conectarse a la instancia de SQL Server instalada en la computadora nueva.
3. Ejecutar el script `FisioSalud_Backup_Esquema_Y_Datos.sql`.
4. Comprobar que la base se llama **FisioSalud** (el script utiliza esa base).
5. Verificar que las tablas se hayan creado correctamente.

El proyecto también contiene `FisioSalud_Actualizacion_NoDestructiva.sql`, destinado a aplicar una actualización puntual sin borrar datos existentes; no sustituye al respaldo principal para una instalación limpia.

### 6.2. Cambiar la cadena de conexión

La aplicación obtiene la conexión desde:

```text
FisioSalud_Proyecto/appsettings.json
```

El proyecto usa la clave:

```json
"ConnectionStrings": {
  "DefaultConnection": "..."
}
```

En otra computadora debes reemplazar el servidor de la cadena por el nombre de la instancia SQL instalada allí.

Ejemplo para SQL Server Express:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=TU-PC\\SQLEXPRESS;Database=FisioSalud;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

Ejemplo si utilizas una instancia por defecto:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=FisioSalud;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

El formato exacto depende de cómo esté instalada tu instancia de SQL Server.

### 6.3. Qué hace el inicializador al arrancar

`Data/DbInitializer.cs` se ejecuta durante el arranque de la aplicación. El inicializador:

- verifica/crea tablas auxiliares como `AjustesSistema`;
- verifica/crea tablas relacionadas con recuperación de contraseña y mensajería;
- verifica/crea asignaciones de pacientes y equipos terapéuticos;
- crea los roles si todavía no existen;
- crea el usuario administrador inicial si no existe;
- carga catálogos iniciales de servicios, patologías y ejercicios;
- registra una caminadora terapéutica de ejemplo si no hay equipos;
- configura disponibilidad inicial para fisioterapeutas existentes;
- realiza algunas asignaciones iniciales de pacientes.

Por esa razón, **primero debe existir la base de datos y el esquema principal**; el inicializador no debe considerarse un sustituto de la restauración del respaldo SQL completo.

## 7. Configurar el correo electrónico

`Startup.cs` registra `SmtpSettings` y el `EmailService`.

La configuración se encuentra en `appsettings.json` bajo:

```json
"SmtpSettings": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "EnableSsl": true,
  "UserName": "TU_CORREO",
  "Password": "TU_CREDENCIAL",
  "FromEmail": "TU_CORREO",
  "FromName": "FisioSalud"
}
```

Esto se utiliza para funciones como recuperación de contraseña.

### Seguridad importante

El `appsettings.json` de la rama revisada contiene una credencial SMTP escrita directamente en el repositorio. **No se debe reutilizar ni publicar esa credencial.** Antes de usar el proyecto en un entorno real:

1. Revoca/cambia la contraseña o credencial expuesta.
2. Coloca las nuevas credenciales mediante variables de entorno, Secret Manager u otro mecanismo seguro.
3. Evita volver a subir contraseñas al repositorio.

## 8. Configuración de la URL de la aplicación

En `appsettings.json` existe:

```json
"AppSettings": {
  "BaseUrl": "https://localhost:5001"
}
```

Esta URL se utiliza, entre otras cosas, para construir el enlace de recuperación de contraseña. Si la aplicación se ejecuta con otra URL/puerto, debe actualizarse este valor para que los enlaces generados apunten al sitio correcto.

## 9. Restaurar paquetes y compilar

Desde la carpeta donde está el archivo `.sln`:

```bash
dotnet restore
```

Después:

```bash
dotnet build FisioSalud_Proyecto.sln
```

Si la compilación termina correctamente, el proyecto ya está listo para ejecutarse.

## 10. Ejecutar la aplicación

### Opción A: Visual Studio

1. Abrir `FisioSalud_Proyecto.sln`.
2. Seleccionar `FisioSalud_Proyecto` como proyecto de inicio.
3. Comprobar que la cadena de conexión sea válida.
4. Comprobar SQL Server y que la base `FisioSalud` esté disponible.
5. Ejecutar con **F5** o **Ctrl + F5**.

### Opción B: Terminal

Entrar a la carpeta del proyecto web:

```bash
cd FisioSalud_Proyecto
```

Ejecutar:

```bash
dotnet run
```

La aplicación utiliza HTTPS y la URL concreta puede variar según el perfil/entorno configurado por Visual Studio o Kestrel. La configuración del proyecto utiliza HTTPS, redirección a HTTPS y la ruta MVC por defecto.

## 11. Qué ocurre al iniciar

El flujo técnico de arranque es:

```text
Program.cs
   ↓
CreateHostBuilder()
   ↓
Startup.cs
   ↓
Configuración de servicios
   ↓
Entity Framework Core + SQL Server
   ↓
Autenticación/Autorización + Session
   ↓
DbInitializer.InitializeAsync()
   ↓
Aplicación web disponible
```

La ruta MVC predeterminada es:

```text
{controller=Home}/{action=Index}/{id?}
```

También se habilitan archivos estáticos, sesión, autenticación por cookie y autorización por roles.

## 12. Inicio de sesión y recuperación de contraseña

### Iniciar sesión

El módulo está en:

```text
/Auth/Login
```

El controlador `AuthController` valida las credenciales mediante `AuthService`. Si el inicio de sesión es correcto, el usuario es redirigido al panel correspondiente a su rol.

### Registro

Existe el formulario:

```text
/Auth/Register
```

La aplicación valida el modelo y registra al usuario mediante `AuthService`.

### Recuperar contraseña

El flujo es:

```text
/Auth/ForgotPassword
        ↓
Solicitud de recuperación
        ↓
Generación de token
        ↓
Envío de correo
        ↓
/Auth/ResetPassword?token=...
```

Si SMTP no está configurado correctamente, el código contempla una ruta de prueba para desarrollo.

## 13. Manual de uso por rol

# 13.1. Administrador

El administrador es el usuario con mayor nivel de acceso.

### Gestión de usuarios

Desde el panel administrativo se pueden administrar usuarios y sus roles.

### Gestión de citas

En el módulo de gestión de citas se pueden:

- buscar citas;
- filtrar por estado;
- filtrar por rango de fechas;
- cambiar el estado de una cita;
- cancelar una cita;
- marcar una cita como no asistida.

### Facturación y pagos

El administrador puede consultar facturas y registrar/verificar pagos.

Entre las acciones implementadas están:

- registrar pago;
- cancelar factura;
- marcar factura como reembolsada;
- revisar el estado de los pagos.

El módulo de cobros también restringe la información para que un usuario no administrador vea únicamente sus propios pagos.

### Servicios

Permite administrar servicios del centro, incluyendo su nombre, descripción, precio, tipo y estado.

### Disponibilidad de fisioterapeutas

Permite definir horarios disponibles por fisioterapeuta y día de la semana.

### Catálogo clínico

El panel administrativo incluye gestión de:

- ejercicios;
- patologías;
- otros catálogos utilizados por el sistema.

### Equipos terapéuticos

Permite controlar equipos, incluyendo disponibilidad y reservas/uso del equipo.

## 13.2. Fisioterapeuta

El fisioterapeuta trabaja sobre los pacientes que tiene asignados.

### Pacientes

Puede:

- crear pacientes;
- editar pacientes;
- consultar el detalle del paciente;
- revisar pacientes asignados;
- iniciar una atención.

El código verifica que el fisioterapeuta tenga acceso al paciente antes de permitir ciertas operaciones.

### Agenda

Puede consultar la agenda y gestionar la atención asociada a sus pacientes y citas.

También puede cancelar o registrar citas según las funciones disponibles para su rol.

### Evaluación inicial

El flujo clínico incluye el registro de una evaluación inicial vinculada al paciente, fisioterapeuta y, cuando corresponde, a una cita.

Los campos contemplados en el modelo incluyen motivo de consulta, antecedentes, dolor inicial, evaluación física, observaciones y fechas de evaluación/registro.

### Diagnóstico y plan de tratamiento

Los modelos y controladores del sistema permiten trabajar con:

```text
Paciente
  ↓
Diagnóstico
  ↓
Plan de tratamiento
  ↓
Ejercicios asignados
  ↓
Sesiones de rehabilitación
  ↓
Seguimiento
```

El módulo de planes de tratamiento permite cambiar el estado del plan. Los estados contemplados en el controlador son:

- `ACTIVO`
- `PAUSADO`
- `COMPLETADO`
- `CANCELADO`

Además, el acceso al plan se restringe al fisioterapeuta correspondiente.

### Sesiones y ejercicios

La base de datos contempla el registro de ejercicios realizados por el paciente y datos como series, repeticiones, dolor, estado, comentarios y fecha de realización.

## 13.3. Cliente / Paciente

El cliente puede consultar la información asociada a su cuenta.

### Perfil

Puede actualizar información de su perfil. El controlador sincroniza la información relevante entre `Pacientes` y `Usuarios`.

### Citas

Puede consultar la información de sus citas según las funciones habilitadas en su módulo.

### Facturas y pagos

Puede consultar **Mis Facturas** y registrar un pago/comprobante. El sistema indica que la administración debe verificar el ingreso antes de confirmar el pago y la cita.

### Contraseña

Puede cambiar su contraseña desde la configuración.

### Actividades / ejercicios

La estructura del cliente incluye acciones para registrar el cumplimiento de ejercicios indicados dentro del tratamiento.

## 14. Módulos funcionales detectados

| Módulo | Qué gestiona |
|---|---|
| Autenticación | Login, registro, logout, recuperación y restablecimiento de contraseña. |
| Usuarios | Datos de usuarios y roles. |
| Pacientes | Registro, edición, consulta y asignación. |
| Agenda | Creación, reprogramación, cancelación y estados de citas. |
| Evaluación clínica | Evaluación inicial y datos clínicos. |
| Diagnóstico | Diagnósticos asociados a paciente y fisioterapeuta. |
| Planes | Planes de tratamiento y ejercicios asignados. |
| Rehabilitación | Sesiones, tratamientos y ejercicios realizados. |
| Seguimiento | Evolución clínica y métricas de recuperación. |
| Servicios | Catálogo de servicios y precios. |
| Facturación | Facturas, detalles y estados. |
| Pagos | Registro/verificación de pagos. |
| Mensajería | Mensajes entre usuarios. |
| Equipos | Equipos terapéuticos y su uso/reserva. |
| Disponibilidad | Horarios de los fisioterapeutas. |
| Ajustes | Configuraciones del sistema. |

## 15. Modelo de datos principal

`FisioSaludDbContext` expone entidades para:

- Roles
- Ajustes del sistema
- Usuarios
- Pacientes
- Citas
- Facturas
- Detalles de factura
- Tokens de recuperación de contraseña
- Patologías
- Diagnósticos
- Planes de tratamiento
- Ejercicios
- Ejercicios asignados al tratamiento
- Sesiones de rehabilitación
- Seguimientos
- Mensajes
- Evaluaciones iniciales
- Servicios
- Pagos
- Tratamientos aplicados
- Tratamientos de sesión
- Ejercicios realizados
- Disponibilidad de fisioterapeutas
- Asignaciones paciente/fisioterapeuta
- Equipos terapéuticos
- Uso de equipos

Entity Framework Core configura varias relaciones con claves foráneas y restricciones de borrado para proteger la integridad de los datos clínicos y administrativos.

## 16. Flujo de una atención de fisioterapia

Un flujo típico de uso sería:

```text
1. El administrador registra/configura usuarios y catálogo.
2. Se registra un paciente.
3. Se asigna el paciente a un fisioterapeuta.
4. Se agenda una cita.
5. El fisioterapeuta inicia la atención.
6. Se registra la evaluación inicial.
7. Se registra diagnóstico.
8. Se crea el plan de tratamiento.
9. Se asignan ejercicios.
10. Se registran sesiones y tratamientos aplicados.
11. El paciente registra actividades/ejercicios realizados.
12. Se registran seguimientos de evolución.
13. Administración gestiona factura y pago.
```

## 17. Problemas comunes al instalarlo en otra PC

### Error de conexión a SQL Server

Revisar:

- que el servicio de SQL Server esté iniciado;
- que la instancia utilizada en la cadena de conexión exista;
- que `Database=FisioSalud` sea correcto;
- que el usuario de Windows tenga acceso a la base cuando se usa `Trusted_Connection=True`;
- que `TrustServerCertificate=True` coincida con el entorno local utilizado.

### La aplicación compila, pero falla al arrancar por la base

Revisar el mensaje de error del log y confirmar que la base `FisioSalud` haya sido restaurada desde el script SQL antes del primer arranque.

### No llegan correos

Revisar `SmtpSettings`, credenciales, puerto 587, SSL y las restricciones de la cuenta de correo utilizada.

### La URL de recuperación no funciona

Comprobar `AppSettings:BaseUrl` en `appsettings.json`.

### No aparecen opciones de administración

Verificar que la cuenta haya sido creada con el rol correcto. El sistema controla el acceso mediante `[Authorize]` y políticas/roles.

## 18. Recomendación para transportar el proyecto a otra computadora

Para una migración ordenada, llevar estos elementos:

```text
FisioSalud_Proyecto.sln
FisioSalud_Proyecto/
FisioSalud_Backup_Esquema_Y_Datos.sql
FisioSalud_Actualizacion_NoDestructiva.sql
```

No es necesario transportar carpetas generadas como `bin` y `obj`; pueden volver a generarse al restaurar/compilar el proyecto.

Después:

```text
1. Instalar .NET 5 SDK
2. Instalar SQL Server
3. Crear/restaurar la base FisioSalud
4. Cambiar DefaultConnection
5. Revisar configuración SMTP
6. Ejecutar dotnet restore
7. Ejecutar dotnet build
8. Ejecutar dotnet run
9. Probar Login
10. Probar módulos según el rol
```

## 19. Verificación después de instalar

Antes de considerar la instalación terminada, hacer esta prueba mínima:

### Prueba de infraestructura

- La aplicación compila.
- SQL Server responde.
- La aplicación se conecta a `FisioSalud`.
- La página inicial abre correctamente.

### Prueba de autenticación

- Iniciar sesión como administrador.
- Cerrar sesión.
- Verificar que las áreas protegidas no sean accesibles sin autenticación.

### Prueba funcional del administrador

- Abrir gestión de usuarios.
- Abrir gestión de citas.
- Abrir facturación/pagos.
- Abrir servicios.
- Abrir equipos.

### Prueba funcional del fisioterapeuta

- Consultar pacientes.
- Crear/editar un paciente.
- Abrir el detalle del paciente.
- Crear o consultar una cita.
- Registrar evaluación/atención.
- Crear/consultar plan de tratamiento.

### Prueba funcional del cliente

- Abrir perfil.
- Consultar citas.
- Consultar facturas.
- Registrar un pago.
- Cambiar contraseña.

## 20. Puntos técnicos importantes para mantenimiento

- `Program.cs` crea el host y delega la configuración a `Startup`.
- `Startup.cs` registra Entity Framework Core, servicios de negocio, autenticación, autorización, MVC y sesión.
- `FisioSaludDbContext.cs` define el modelo de datos y sus relaciones.
- `DbInitializer.cs` realiza inicialización y carga de catálogos.
- La autenticación utiliza cookie y no JWT.
- La aplicación utiliza MVC tradicional con Razor Views.
- Los servicios separan buena parte de la lógica de negocio de los controladores.

## 21. Seguridad antes de ponerlo en producción

Esta versión debe considerarse un proyecto que requiere endurecimiento antes de producción.

Como mínimo:

1. Eliminar credenciales SMTP del repositorio.
2. Cambiar la contraseña del usuario administrador inicial.
3. Configurar secretos mediante variables de entorno/secret manager.
4. Revisar HTTPS y certificados reales.
5. Revisar políticas de copia de seguridad de SQL Server.
6. Evitar almacenar datos clínicos reales en un repositorio público.
7. Revisar permisos de SQL Server y cuentas de servicio.
8. Actualizar el framework/proyecto a una versión de .NET con soporte vigente antes de un despliegue serio.

## 22. Archivos clave de referencia

| Archivo | Propósito |
|---|---|
| `FisioSalud_Proyecto.sln` | Solución de Visual Studio. |
| `FisioSalud_Proyecto/FisioSalud_Proyecto.csproj` | Framework y paquetes NuGet. |
| `FisioSalud_Proyecto/Program.cs` | Punto de entrada. |
| `FisioSalud_Proyecto/Startup.cs` | Configuración de servicios y middleware. |
| `FisioSalud_Proyecto/Data/FisioSaludDbContext.cs` | Modelo EF Core y relaciones. |
| `FisioSalud_Proyecto/Data/DbInitializer.cs` | Inicialización y catálogos. |
| `FisioSalud_Proyecto/appsettings.json` | Conexión, SMTP y configuración general. |
| `FisioSalud_Backup_Esquema_Y_Datos.sql` | Respaldo de esquema y datos. |
| `FisioSalud_Actualizacion_NoDestructiva.sql` | Actualización SQL no destructiva. |

## 23. Resumen rápido: instalación desde cero

```text
┌─────────────────────────────────────┐
│ 1. Clonar/descargar Fisio_Salud     │
└──────────────────┬──────────────────┘
                   ↓
┌─────────────────────────────────────┐
│ 2. Instalar .NET 5 + SQL Server     │
└──────────────────┬──────────────────┘
                   ↓
┌─────────────────────────────────────┐
│ 3. Ejecutar/restaurar el SQL         │
│    FisioSalud_Backup_Esquema...sql  │
└──────────────────┬──────────────────┘
                   ↓
┌─────────────────────────────────────┐
│ 4. Editar DefaultConnection         │
└──────────────────┬──────────────────┘
                   ↓
┌─────────────────────────────────────┐
│ 5. Revisar SMTP y BaseUrl           │
└──────────────────┬──────────────────┘
                   ↓
┌─────────────────────────────────────┐
│ 6. dotnet restore                   │
│ 7. dotnet build                     │
└──────────────────┬──────────────────┘
                   ↓
┌─────────────────────────────────────┐
│ 8. dotnet run / F5                  │
└──────────────────┬──────────────────┘
                   ↓
┌─────────────────────────────────────┐
│ 9. DbInitializer prepara auxiliares │
└──────────────────┬──────────────────┘
                   ↓
┌─────────────────────────────────────┐
│ 10. Probar login y módulos          │
└─────────────────────────────────────┘
```

## 24. Referencia del repositorio

Repositorio original revisado:

`https://github.com/Adonis2005/Fisio_Salud.git`

Rama revisada:

`v0/completar-modelo-fisiosalud`
