using Fap.Domain.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Fap.Api.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
        Task SendOtpEmailAsync(string toEmail, string otp, string purpose);
        Task SendWelcomeEmailAsync(string toEmail, string fullName, string password);
        Task SendPasswordResetEmailAsync(string toEmail, string otp);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlBody
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation($"Email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send email to {toEmail}: {ex.Message}");
                throw;
            }
        }

        public async Task SendOtpEmailAsync(string toEmail, string otp, string purpose)
        {
            var subject = purpose == "Registration" ? "Verify Your Email - OTP Code" : "Password Reset - OTP Code";
            var htmlBody = GetOtpEmailTemplate(otp, purpose);
            await SendEmailAsync(toEmail, subject, htmlBody);
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string fullName, string password)
        {
            var subject = "Welcome to UAP System - Account Created";
            var htmlBody = GetWelcomeEmailTemplate(fullName, toEmail, password);
            await SendEmailAsync(toEmail, subject, htmlBody);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string otp)
        {
            var subject = "Password Reset Request - OTP Code";
            var htmlBody = GetPasswordResetEmailTemplate(otp);
            await SendEmailAsync(toEmail, subject, htmlBody);
        }

        private string GetOtpEmailTemplate(string otp, string purpose)
        {
            var action = purpose == "Registration" ? "verify your email" : "reset your password";
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .otp-box {{ background: white; border: 2px dashed #667eea; padding: 20px; text-align: center; font-size: 32px; font-weight: bold; color: #667eea; letter-spacing: 8px; margin: 20px 0; border-radius: 5px; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>🔐 OTP Verification</h1>
                        </div>
                        <div class='content'>
                            <p>Hello,</p>
                            <p>You requested to {action}. Please use the following OTP code:</p>
                            <div class='otp-box'>{otp}</div>
                            <p><strong>⏰ This code will expire in 5 minutes.</strong></p>
                            <p>If you didn't request this, please ignore this email.</p>
                        </div>
                        <div class='footer'>
                            <p>© 2025 UAP System - University Academic Management</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private string GetWelcomeEmailTemplate(string fullName, string email, string password)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .credentials {{ background: white; padding: 15px; border-left: 4px solid #667eea; margin: 20px 0; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>🎉 Welcome to UAP System!</h1>
                        </div>
                        <div class='content'>
                            <p>Hello <strong>{fullName}</strong>,</p>
                            <p>Your account has been successfully created by the administrator.</p>
                            <div class='credentials'>
                                <p><strong>📧 Email:</strong> {email}</p>
                                <p><strong>🔑 Temporary Password:</strong> {password}</p>
                            </div>
                            <p><strong>⚠️ Important:</strong> Please change your password after first login for security.</p>
                            <p>If you have any questions, please contact the administrator.</p>
                        </div>
                        <div class='footer'>
                            <p>© 2025 UAP System - University Academic Management</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private string GetPasswordResetEmailTemplate(string otp)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .otp-box {{ background: white; border: 2px dashed #f5576c; padding: 20px; text-align: center; font-size: 32px; font-weight: bold; color: #f5576c; letter-spacing: 8px; margin: 20px 0; border-radius: 5px; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>🔒 Password Reset Request</h1>
                        </div>
                        <div class='content'>
                            <p>Hello,</p>
                            <p>You requested to reset your password. Please use the following OTP code:</p>
                            <div class='otp-box'>{otp}</div>
                            <p><strong>⏰ This code will expire in 5 minutes.</strong></p>
                            <p><strong>⚠️ Security Notice:</strong> If you didn't request this, please contact support immediately.</p>
                        </div>
                        <div class='footer'>
                            <p>© 2025 UAP System - University Academic Management</p>
                        </div>
                    </div>
                </body>
                </html>";
        }
    }
}