using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Id_card.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, string attachmentPath = null)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                _config["SMTP:FromName"],
                _config["SMTP:FromEmail"]
            ));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = body };

            if (!string.IsNullOrEmpty(attachmentPath))
            {
                builder.Attachments.Add(attachmentPath);
            }

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();

            var host = _config["SMTP:Host"];
            var port = int.Parse(_config["SMTP:Port"]);

            // Choose secure socket option based on port or explicit config
            // 587 => STARTTLS, 465 => SSL on connect, others => Auto/None
            var secureOption = SecureSocketOptions.Auto;
            var secureConfig = _config["SMTP:SecureOption"]; // Optional: "StartTls", "SslOnConnect", "None", "Auto"
            if (!string.IsNullOrWhiteSpace(secureConfig))
            {
                secureOption = secureConfig switch
                {
                    "StartTls" => SecureSocketOptions.StartTls,
                    "SslOnConnect" => SecureSocketOptions.SslOnConnect,
                    "None" => SecureSocketOptions.None,
                    _ => SecureSocketOptions.Auto
                };
            }
            else
            {
                secureOption = port switch
                {
                    587 => SecureSocketOptions.StartTls,
                    465 => SecureSocketOptions.SslOnConnect,
                    _ => SecureSocketOptions.Auto
                };
            }

            await client.ConnectAsync(host, port, secureOption);
            // Force basic auth; some providers (e.g., Zoho) reject XOAUTH2 unless configured for OAuth
            client.AuthenticationMechanisms.Remove("XOAUTH2");
            // Prefer LOGIN and PLAIN which are commonly supported
            client.AuthenticationMechanisms.Remove("NTLM");
            client.AuthenticationMechanisms.Remove("GSSAPI");

            var userName = _config["SMTP:UserName"];
            var password = _config["SMTP:Password"];
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("SMTP credentials are missing. Configure SMTP:UserName and SMTP:Password.");
            }

            try
            {
                await client.AuthenticateAsync(userName, password);
                await client.SendAsync(message);
            }
            catch (MailKit.Security.AuthenticationException)
            {
                throw new InvalidOperationException("SMTP authentication failed. Verify username (full email), password/app password, SMTP host for your data center, and that FromEmail matches the account.");
            }
            catch (SmtpCommandException ex)
            {
                throw new InvalidOperationException($"SMTP command failed: {ex.Message}");
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }
    }
}
