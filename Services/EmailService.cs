using GuestHouseBookingCore.Repositories;
using System.Net;
using System.Net.Mail;

namespace GuestHouseBookingCore.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        public EmailService(IConfiguration config) => _config = config;

        public async Task SendWelcomeEmail(string toEmail, string username, string tempPassword)
        {
            var smtp = _config["Email:SmtpServer"];
            var port = int.Parse(_config["Email:Port"]);
            var from = _config["Email:From"];
            var pass = _config["Email:Password"];

            var message = new MailMessage(from, toEmail)
            {
                Subject = "Your Account is Ready!",
                Body = $"<h2>Welcome {username}!</h2><p>Username: <b>{username}</b></p><p>Password: <b>{tempPassword}</b></p><p><a href='http://localhost:4200/change-password'>Change Password</a></p>",
                IsBodyHtml = true
            };

            using var client = new SmtpClient(smtp, port)
            {
                Credentials = new NetworkCredential(from, pass),
                EnableSsl = true
            };

            await client.SendMailAsync(message);
        }

        public async Task SendBookingStatusEmail(
    string toEmail, string userName, string status, string room, string bed,
    DateTime checkIn, DateTime checkOut, string adminName, string? reason = null)
        {
            var fromEmail = _config["Email:From"];
            var password = _config["Email:Password"];
            var smtpServer = _config["Email:SmtpServer"];
            var smtpPort = int.Parse(_config["Email:Port"]);

            var subject = $"Your Booking Request has been {status}!";
            var body = status == "Accepted"
                ? $"<h2>Great News, {userName}!</h2><p>Your booking has been <strong>APPROVED</strong> by <strong>{adminName}</strong>.</p>"
                : $"<h2>Sorry, {userName}</h2><p>Your booking has been <strong>REJECTED</strong> by <strong>{adminName}</strong>.</p><p><strong>Reason:</strong> {reason}</p>";

            body += $"<p><strong>Room:</strong> {room}<br><strong>Bed:</strong> {bed}<br>" +
                    $"<strong>Check-in:</strong> {checkIn:dd MMM yyyy}<br>" +
                    $"<strong>Check-out:</strong> {checkOut:dd MMM yyyy}</p>";

            var message = new MailMessage(fromEmail, toEmail)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            using var client = new SmtpClient(smtpServer, smtpPort)
            {
                Credentials = new NetworkCredential(fromEmail, password),
                EnableSsl = true
            };

            await client.SendMailAsync(message);
        }
    }
}
