using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace SRG.Application.Email;

public interface IEmailService
{
    Task<bool> SendActivationEmailAsync(string toEmail, string toName, string activationToken, DateTime expiresAt, CancellationToken cancellationToken = default);
}

public class EmailService : IEmailService
{
    private readonly string _server;
    private readonly int _port;
    private readonly string _user;
    private readonly string _password;
    private readonly bool _secure;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly string _webOrigin;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _logger = logger;
        _server = configuration["Smtp:Server"] ?? throw new InvalidOperationException("Smtp:Server not configured");
        _port = int.Parse(configuration["Smtp:Port"] ?? "587");
        _user = configuration["Smtp:User"] ?? throw new InvalidOperationException("Smtp:User not configured");
        _password = configuration["Smtp:Password"] ?? throw new InvalidOperationException("Smtp:Password not configured");
        _secure = bool.Parse(configuration["Smtp:Secure"] ?? "false");
        _fromEmail = configuration["Smtp:FromEmail"] ?? _user;
        _fromName = configuration["Smtp:FromName"] ?? "SRG System";
        _webOrigin = configuration["Cors:WebOrigin"] ?? "http://localhost:5173";
    }

    public async Task<bool> SendActivationEmailAsync(string toEmail, string toName, string activationToken, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        try
        {
            var activationUrl = $"{_webOrigin}/auth/activate?token={Uri.EscapeDataString(activationToken)}";
            var expiresAtLocal = expiresAt.ToLocalTime();
            
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_fromName, _fromEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = "Aktywacja konta w systemie SRG";

            var htmlBody = GenerateActivationEmailHtml(toName, activationToken, activationUrl, expiresAtLocal);
            
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody,
                TextBody = $@"Witaj {toName},

Twoje konto w systemie SRG zostało utworzone lub zresetowane.

Twój token aktywacyjny: {activationToken}

Link do aktywacji: {activationUrl}

Token wygasa: {expiresAtLocal:dd.MM.yyyy HH:mm}

Po kliknięciu w link lub wpisaniu tokenu, zostaniesz poproszony o zalogowanie się kontem Microsoft.
Email Twojego konta Microsoft musi być taki sam jak ten, na który otrzymałeś tę wiadomość.

Pozdrawiamy,
Zespół SRG"
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            
            var secureSocketOptions = _secure ? SecureSocketOptions.StartTls : SecureSocketOptions.StartTlsWhenAvailable;
            
            await client.ConnectAsync(_server, _port, secureSocketOptions, cancellationToken);
            await client.AuthenticateAsync(_user, _password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            
            _logger.LogInformation("Activation email sent successfully to {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send activation email to {Email}", toEmail);
            return false;
        }
    }

    private static string GenerateActivationEmailHtml(string userName, string token, string activationUrl, DateTime expiresAt)
    {
        return $@"
<!DOCTYPE html>
<html lang=""pl"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Aktywacja konta SRG</title>
</head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f1f5f9;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 20px;"">
                <table role=""presentation"" style=""width: 100%; max-width: 600px; border-collapse: collapse;"">
                    <!-- Header -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #0ea5e9 0%, #0284c7 100%); padding: 32px 40px; border-radius: 16px 16px 0 0;"">
                            <table role=""presentation"" style=""width: 100%;"">
                                <tr>
                                    <td>
                                        <div style=""display: inline-block; background-color: white; width: 48px; height: 48px; border-radius: 12px; text-align: center; line-height: 48px; font-size: 24px; font-weight: bold; color: #0ea5e9;"">
                                            S
                                        </div>
                                    </td>
                                </tr>
                                <tr>
                                    <td style=""padding-top: 20px;"">
                                        <h1 style=""margin: 0; color: white; font-size: 24px; font-weight: 600;"">
                                            Aktywacja konta
                                        </h1>
                                        <p style=""margin: 8px 0 0 0; color: rgba(255,255,255,0.9); font-size: 16px;"">
                                            System zarządzania SRG
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    
                    <!-- Body -->
                    <tr>
                        <td style=""background-color: white; padding: 40px; border-radius: 0 0 16px 16px; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);"">
                            <p style=""margin: 0 0 24px 0; color: #334155; font-size: 16px; line-height: 1.6;"">
                                Witaj <strong>{userName}</strong>,
                            </p>
                            <p style=""margin: 0 0 24px 0; color: #334155; font-size: 16px; line-height: 1.6;"">
                                Twoje konto w systemie SRG zostało utworzone lub zresetowane. Aby się zalogować, użyj poniższego tokenu aktywacyjnego.
                            </p>
                            
                            <!-- Token Box -->
                            <div style=""background-color: #f8fafc; border: 2px dashed #cbd5e1; border-radius: 12px; padding: 24px; text-align: center; margin-bottom: 24px;"">
                                <p style=""margin: 0 0 8px 0; color: #64748b; font-size: 12px; text-transform: uppercase; letter-spacing: 1px; font-weight: 600;"">
                                    Twój token aktywacyjny
                                </p>
                                <p style=""margin: 0; font-family: 'Courier New', monospace; font-size: 18px; color: #0f172a; font-weight: bold; word-break: break-all;"">
                                    {token}
                                </p>
                            </div>
                            
                            <!-- CTA Button -->
                            <div style=""text-align: center; margin-bottom: 24px;"">
                                <a href=""{activationUrl}"" style=""display: inline-block; background: linear-gradient(135deg, #0ea5e9 0%, #0284c7 100%); color: white; text-decoration: none; padding: 14px 32px; border-radius: 10px; font-weight: 600; font-size: 16px;"">
                                    Aktywuj konto
                                </a>
                            </div>
                            
                            <!-- Warning -->
                            <div style=""background-color: #fef3c7; border-left: 4px solid #f59e0b; padding: 16px; border-radius: 0 8px 8px 0; margin-bottom: 24px;"">
                                <p style=""margin: 0; color: #92400e; font-size: 14px;"">
                                    <strong>⏱️ Token wygasa:</strong> {expiresAt:dd.MM.yyyy} o godz. {expiresAt:HH:mm}
                                </p>
                            </div>
                            
                            <!-- Instructions -->
                            <div style=""background-color: #eff6ff; border-radius: 12px; padding: 20px; margin-bottom: 24px;"">
                                <p style=""margin: 0 0 12px 0; color: #1e40af; font-size: 14px; font-weight: 600;"">
                                    📋 Instrukcja aktywacji:
                                </p>
                                <ol style=""margin: 0; padding-left: 20px; color: #1e40af; font-size: 14px; line-height: 1.8;"">
                                    <li>Kliknij przycisk ""Aktywuj konto"" powyżej</li>
                                    <li>Zaloguj się kontem Microsoft</li>
                                    <li>Email konta Microsoft musi być: <strong>{userName}</strong></li>
                                </ol>
                            </div>
                            
                            <p style=""margin: 0; color: #64748b; font-size: 14px; line-height: 1.6;"">
                                Jeśli nie prosiłeś o utworzenie konta, zignoruj tę wiadomość.
                            </p>
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style=""padding: 24px 40px; text-align: center;"">
                            <p style=""margin: 0; color: #94a3b8; font-size: 12px;"">
                                © {DateTime.Now.Year} SRG System. Wszystkie prawa zastrzeżone.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }
}
