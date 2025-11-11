    using MailKit.Net.Smtp;
    using MimeKit;
    using RequisicionesApi.Interfaces;
    using RequisicionesApi.Models;
using RequisicionesApi.Utilidades;
using System.Net.Mail;
using System.Security.Cryptography.X509Certificates;
using static Org.BouncyCastle.Math.EC.ECCurve;
namespace RequisicionesApi.Services
{

    public class MailService : IMailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<MailService> _logger;

        public MailService(IConfiguration config, ILogger<MailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<MailResponse> EnviarCorreoAdjudicacionAsync(ProductoAdjudicadoDTO productoadjudicado, string mailusuario )
        {
            var response = new MailResponse();

            try
            {
                // Construir el DTO completo para el correo
                //var dto = new ProductoAdjudicadoDTO
                //{
                //    CorreoProveedor = destinatario,
                //    RequisicionId = reqIdClave,
                //    NombreProveedor = nombreprovedor, // Puedes parametrizar esto si lo tienes
                //    IdProveedor = "ID",            // Igual aquí
                //    Productos = productos
                //};

                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_config["MailSettings:From"]));
                email.To.Add(MailboxAddress.Parse(productoadjudicado.CorreoProveedor));
                email.To.Add(MailboxAddress.Parse(mailusuario));
                email.Subject = $"Adjudicación – Requisición {productoadjudicado.RequisicionId}";

                var builder = new BodyBuilder
                {
                    HtmlBody = MailBuilder.GenerarCorreoAdjudicacion(productoadjudicado)
                };

                email.Body = builder.ToMessageBody();

                using var smtp = new MailKit.Net.Smtp.SmtpClient();
                await smtp.ConnectAsync(
                    _config["MailSettings:Host"],
                    int.Parse( _config["MailSettings:Port"]),
                    MailKit.Security.SecureSocketOptions.StartTls);

                await smtp.AuthenticateAsync(
                    _config["MailSettings:Username"],
                    _config["MailSettings:Password"]);

                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                response.Success = true;
                response.Message = "Correo enviado correctamente";

                _logger.LogInformation("Correo enviado a {destinatario} para ReqIdClave {reqId}", productoadjudicado.CorreoProveedor, productoadjudicado.RequisicionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correo de adjudicación a {destinatario}", productoadjudicado.CorreoProveedor  );
                response.Success = false;
                response.Message = $"Error: {ex.Message}";
            }

            return response;
        }

        public async Task<MailResponse> EnviarCorreoInvitacionAsync(string prov, string subject, string html, string tocorreo)
        {
            var response = new MailResponse();

            try
            {
                // ===== Validaciones de entrada =====
                if (string.IsNullOrWhiteSpace(tocorreo))
                    throw new ArgumentException("El correo de destino (tocorreo) es requerido.", nameof(tocorreo));

                subject ??= string.Empty;
                html ??= string.Empty;

                // ===== Validaciones de configuración =====
                if (_config is null)
                    throw new InvalidOperationException("IConfiguration (_config) no fue inyectado en MailService.");

                string? from = _config["MailSettings:From"];
                string? host = _config["MailSettings:Host"];
                string? portStr = _config["MailSettings:Port"];
                string? username = _config["MailSettings:Username"];
                string? password = _config["MailSettings:Password"];
                string? fromName = _config["MailSettings:FromName"];     // opcional
                string? startTls = _config["MailSettings:UseStartTls"];  // opcional

                if (string.IsNullOrWhiteSpace(from)) throw new InvalidOperationException("MailSettings:From no está configurado.");
                if (string.IsNullOrWhiteSpace(host)) throw new InvalidOperationException("MailSettings:Host no está configurado.");
                if (string.IsNullOrWhiteSpace(portStr)) throw new InvalidOperationException("MailSettings:Port no está configurado.");
                if (string.IsNullOrWhiteSpace(username)) throw new InvalidOperationException("MailSettings:Username no está configurado.");
                if (string.IsNullOrWhiteSpace(password)) throw new InvalidOperationException("MailSettings:Password no está configurado.");

                if (!int.TryParse(portStr, out var port))
                    throw new InvalidOperationException("MailSettings:Port no es un número válido.");

                var useStartTls = true;
                if (bool.TryParse(startTls, out var parsed)) useStartTls = parsed;

                // ===== Construcción del mensaje =====
                var email = new MimeMessage();
                if (!string.IsNullOrWhiteSpace(fromName))
                    email.From.Add(new MailboxAddress(fromName, from));
                else
                    email.From.Add(MailboxAddress.Parse(from));

                email.To.Add(MailboxAddress.Parse(tocorreo));
                email.Subject = subject;

                var builder = new BodyBuilder { HtmlBody = html };
                email.Body = builder.ToMessageBody();

                // ===== Envío =====
                using var smtp = new MailKit.Net.Smtp.SmtpClient();

                var socket = useStartTls
                    ? MailKit.Security.SecureSocketOptions.StartTls
                    : MailKit.Security.SecureSocketOptions.Auto;

                await smtp.ConnectAsync(host, port, socket).ConfigureAwait(false);
                await smtp.AuthenticateAsync(username, password).ConfigureAwait(false);
                await smtp.SendAsync(email).ConfigureAwait(false);
                await smtp.DisconnectAsync(true).ConfigureAwait(false);

                response.Success = true;
                response.Message = "Correo enviado correctamente";

                _logger?.LogInformation("Correo enviado a {destinatario} (prov: {prov}) asunto:'{subject}'",
                    tocorreo, prov, subject);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error al enviar correo de invitación a {destinatario}", tocorreo);
                response.Success = false;
                response.Message = $"Error: {ex.Message}";
            }

            return response;
        }


        public async Task<MailResponse> EnviarCorreoUsuAutAsyncTask(ProductoAdjudicadoDTO productoadjudicado, List<RequisicionDetUsuAut> usuaarioDTO, string emailusuario)
        {
            var response = new MailResponse();

            try
            {
                // Construir el DTO completo para el correo
                //var dto = new ProductoAdjudicadoDTO
                //{
                //    CorreoProveedor = destinatario,
                //    RequisicionId = reqIdClave,
                //    NombreProveedor = nombreprovedor, // Puedes parametrizar esto si lo tienes
                //    IdProveedor = "ID",            // Igual aquí
                //    Productos = productos
                //};

                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_config["MailSettings:From"]));

                //email.To.Add(MailboxAddress.Parse(usuaarioDTO.CorreoProveedor));

                foreach (var item in usuaarioDTO)
                {
                    if (item.UsrNivel.Trim() == "L1")
                    {
                        email.To.Add(MailboxAddress.Parse(item.UsrCorreo));
                    }
                }


                email.To.Add(MailboxAddress.Parse(emailusuario));
                email.Subject = $"Solicitud de Autorizacion – Requisición {usuaarioDTO[0].ReqIdClave}";

                var builder = new BodyBuilder
                {
                    HtmlBody = MailBuilder.GenerarCorreoUsuarioAut(productoadjudicado)

                };

                email.Body = builder.ToMessageBody();

                using var smtp = new MailKit.Net.Smtp.SmtpClient();
                await smtp.ConnectAsync(
                    _config["MailSettings:Host"],
                    int.Parse(_config["MailSettings:Port"]),
                    MailKit.Security.SecureSocketOptions.StartTls);

                await smtp.AuthenticateAsync(
                    _config["MailSettings:Username"],
                    _config["MailSettings:Password"]);

                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                response.Success = true;
                response.Message = "Correo enviado correctamente";

                _logger.LogInformation("Correo enviado a {destinatario} para ReqIdClave {reqId}", productoadjudicado.CorreoProveedor, productoadjudicado.RequisicionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correo de adjudicación a {destinatario}", productoadjudicado.CorreoProveedor);
                response.Success = false;
                response.Message = $"Error: {ex.Message}";
            }

            return response;

        }


        public async Task<MailResponse> EnviarCorreoUsuAutLiberar(ProductoAdjudicadoDTO productoadjudicado, List<RequisicionDetUsuAut> usuaarioDTO, string emailusuario)
        {
            var response = new MailResponse();

            try
            {
                // Construir el DTO completo para el correo
                //var dto = new ProductoAdjudicadoDTO
                //{
                //    CorreoProveedor = destinatario,
                //    RequisicionId = reqIdClave,
                //    NombreProveedor = nombreprovedor, // Puedes parametrizar esto si lo tienes
                //    IdProveedor = "ID",            // Igual aquí
                //    Productos = productos
                //};

                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_config["MailSettings:From"]));

                //email.To.Add(MailboxAddress.Parse(usuaarioDTO.CorreoProveedor));
                string nombre = ""; 
                 foreach (var item in usuaarioDTO)
                {
                    if (item.UsrCorreo == emailusuario) {
                        email.To.Add(MailboxAddress.Parse(item.UsrCorreo));
                        nombre =   item.UsrNombre +  " " + item.UsrApellidoP + " " + item.UsrApellidoM;  
                    }
                }


                email.To.Add(MailboxAddress.Parse(emailusuario));
                email.Subject = $"Solicitud de Autorizacion – Requisición {usuaarioDTO[0].ReqIdClave}";

                var builder = new BodyBuilder
                {
                    HtmlBody = MailBuilder.GenerarCorreoUsuarioAutNivel(productoadjudicado, nombre)

                };

                email.Body = builder.ToMessageBody();

                using var smtp = new MailKit.Net.Smtp.SmtpClient();
                await smtp.ConnectAsync(
                    _config["MailSettings:Host"],
                    int.Parse(_config["MailSettings:Port"]),
                    MailKit.Security.SecureSocketOptions.StartTls);

                await smtp.AuthenticateAsync(
                    _config["MailSettings:Username"],
                    _config["MailSettings:Password"]);

                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                response.Success = true;
                response.Message = "Correo enviado correctamente";

                _logger.LogInformation("Correo enviado a {destinatario} para ReqIdClave {reqId}", productoadjudicado.CorreoProveedor, productoadjudicado.RequisicionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correo de adjudicación a {destinatario}", productoadjudicado.CorreoProveedor);
                response.Success = false;
                response.Message = $"Error: {ex.Message}";
            }

            return response;

        }





        public async Task<MailResponse> EnviarCorreoInvitacionAsync2(string prov,string subject,string html,string tocorreo)
        {
            var response = new MailResponse();

            try
            {
                // Construir el DTO completo para el correo
                //var dto = new ProductoAdjudicadoDTO
                //{
                //    CorreoProveedor = destinatario,
                //    RequisicionId = reqIdClave,
                //    NombreProveedor = nombreprovedor, // Puedes parametrizar esto si lo tienes
                //    IdProveedor = "ID",            // Igual aquí
                //    Productos = productos
                //};

                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_config["MailSettings:From"]));
                email.To.Add(MailboxAddress.Parse(tocorreo));
                //email.To.Add(MailboxAddress.Parse(mailusuario));
                email.Subject = subject;

                var builder = new BodyBuilder
                {
                    HtmlBody = html
                };

                email.Body = builder.ToMessageBody();

                using var smtp = new MailKit.Net.Smtp.SmtpClient();
                await smtp.ConnectAsync(
                    _config["MailSettings:Host"],
                    int.Parse(_config["MailSettings:Port"]),
                    MailKit.Security.SecureSocketOptions.StartTls);

                await smtp.AuthenticateAsync(
                    _config["MailSettings:Username"],
                    _config["MailSettings:Password"]);

                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                response.Success = true;
                response.Message = "Correo enviado correctamente";

                _logger?.LogInformation("Correo enviado a {destinatario} para ReqIdClave {reqId}",  tocorreo, subject );
            }
            catch (Exception ex)
            {
                 _logger?.LogError(ex, "Error al enviar correo de adjudicación a {destinatario}", tocorreo);
                response.Success = false;
                response.Message = $"Error: {ex.Message}";
            }

            return response;
        }

 
    }
}
