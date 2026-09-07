using System.Threading.Tasks;
using FisioSalud_Proyecto.Models.Contacto;

namespace FisioSalud_Proyecto.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string resetLink);
        Task SendAppointmentRequestEmailAsync(ReservarCitaViewModel model);
    }

    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _settings;

        public EmailService(SmtpSettings settings)
        {
            _settings = settings;
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            if (string.IsNullOrWhiteSpace(_settings.UserName) || string.IsNullOrWhiteSpace(_settings.Password))
            {
                throw new InvalidSmtpConfigurationException(
                    "La configuración SMTP no está completa. Configure SmtpSettings en appsettings.json.");
            }

            using var client = new System.Net.Mail.SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new System.Net.NetworkCredential(_settings.UserName, _settings.Password)
            };

            var from = string.IsNullOrWhiteSpace(_settings.FromEmail) ? _settings.UserName : _settings.FromEmail;
            var message = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(from, _settings.FromName),
                Subject = "Restablecer contraseña - FisioSalud",
                Body = $@"<html><body>
                    <h2>Restablecer contraseña</h2>
                    <p>Recibimos una solicitud para restablecer su contraseña en FisioSalud.</p>
                    <p><a href=""{resetLink}"">Haga clic aquí para restablecer su contraseña</a></p>
                    <p>Este enlace expira en 1 hora. Si no solicitó este cambio, ignore este correo.</p>
                    </body></html>",
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
        }

        public async Task SendAppointmentRequestEmailAsync(ReservarCitaViewModel model)
        {
            if (string.IsNullOrWhiteSpace(_settings.UserName) || string.IsNullOrWhiteSpace(_settings.Password))
            {
                throw new InvalidSmtpConfigurationException(
                    "La configuración SMTP no está completa. Configure SmtpSettings en appsettings.json.");
            }

            using var client = new System.Net.Mail.SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new System.Net.NetworkCredential(_settings.UserName, _settings.Password)
            };

            var from = string.IsNullOrWhiteSpace(_settings.FromEmail) ? _settings.UserName : _settings.FromEmail;
            var fechaTexto = model.Fecha?.ToString("dd/MM/yyyy") ?? "No especificada";

            var message = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(from, _settings.FromName),
                Subject = $"Nueva solicitud de cita - {model.Nombre} {model.Apellido}",
                Body = $@"<html><body style=""font-family:Arial,sans-serif;color:#333;"">
                    <h2 style=""color:#3C2A21;"">Nueva solicitud de cita</h2>
                    <table style=""border-collapse:collapse;width:100%;"">
                        <tr><td style=""padding:8px;font-weight:bold;"">Nombre:</td><td style=""padding:8px;"">{model.Nombre} {model.Apellido}</td></tr>
                        <tr><td style=""padding:8px;font-weight:bold;"">Email:</td><td style=""padding:8px;"">{model.Email}</td></tr>
                        <tr><td style=""padding:8px;font-weight:bold;"">Teléfono:</td><td style=""padding:8px;"">{model.Telefono}</td></tr>
                        <tr><td style=""padding:8px;font-weight:bold;"">Servicio:</td><td style=""padding:8px;"">{model.Servicio}</td></tr>
                        <tr><td style=""padding:8px;font-weight:bold;"">Fecha:</td><td style=""padding:8px;"">{fechaTexto}</td></tr>
                        <tr><td style=""padding:8px;font-weight:bold;"">Hora:</td><td style=""padding:8px;"">{model.Hora}</td></tr>
                        <tr><td style=""padding:8px;font-weight:bold;"">Mensaje:</td><td style=""padding:8px;"">{model.Mensaje ?? "Sin mensaje adicional"}</td></tr>
                    </table>
                    </body></html>",
                IsBodyHtml = true
            };

            message.To.Add(from);
            message.ReplyToList.Add(new System.Net.Mail.MailAddress(model.Email, $"{model.Nombre} {model.Apellido}"));

            await client.SendMailAsync(message);
        }
    }

    public class SmtpSettings
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public bool EnableSsl { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string FromEmail { get; set; }
        public string FromName { get; set; }
    }

    public class InvalidSmtpConfigurationException : System.Exception
    {
        public InvalidSmtpConfigurationException(string message) : base(message) { }
    }
}
