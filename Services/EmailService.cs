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

        public async Task SendNewBookingAlertToAdmin(
    string toEmail, string userName, string guestHouse, string room, string bed,
    DateTime checkIn, DateTime checkOut, string purpose)
        {
            var fromEmail = _config["Email:From"];
            var password = _config["Email:Password"];
            var smtpServer = _config["Email:SmtpServer"];
            var smtpPort = int.Parse(_config["Email:Port"]);

            // DEBUG LOG
            Console.WriteLine($"Sending email from: {fromEmail} to: {toEmail}");

            var subject = "New Booking Request!";
            var body = $"<h3>New Booking Alert!</h3>" +
                       $"<p><b>User:</b> {userName}</p>" +
                       $"<p><b>Guest House:</b> {guestHouse}<br>" +
                       $"<b>Room:</b> {room}<br>" +
                       $"<b>Bed:</b> {bed}<br>" +
                       $"<b>Check-in:</b> {checkIn:dd MMM yyyy}<br>" +
                       $"<b>Check-out:</b> {checkOut:dd MMM yyyy}</p>" +
                       $"<p><b>Purpose:</b> {purpose}</p>" +
                       $"<p><a href='https://localhost:4200/admin/bookings'>View in Admin Panel</a></p>";

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

            try
            {
                await client.SendMailAsync(message);
                Console.WriteLine($"EMAIL SENT to {toEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EMAIL FAILED: {ex.Message}");
                throw; // DEBUG KE LIYE THROW KAR
            }
        }

        public async Task SendPasswordChangedEmail(string toEmail, string userName)
        {
            var fromEmail = _config["Email:From"];
            var password = _config["Email:Password"];
            var smtpServer = _config["Email:SmtpServer"];
            var smtpPort = int.Parse(_config["Email:Port"]);

            var subject = "Password Changed Successfully";
            var body = $@"
        <h2>Hello {userName},</h2>
        <p>Your password has been <strong>successfully changed</strong>.</p>
        <p><strong>Time:</strong> {DateTime.Now:dd MMM yyyy, hh:mm tt} (IST)</p>
        <p>If you did not make this change, please contact admin immediately.</p>
        <hr>
        <p><small>Guest House Booking System</small></p>";

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

        public async Task SendPasswordResetEmail(string toEmail, string userName, string resetLink)
        {
            var fromEmail = _config["Email:From"];
            var password = _config["Email:Password"];
            var smtpServer = _config["Email:SmtpServer"];
            var smtpPort = int.Parse(_config["Email:Port"]);

            var subject = "Reset Your Password";
            var body = $@"
        <h2>Hello {userName},</h2>
        <p>We received a request to reset your password.</p>
        <p><a href='{resetLink}' style='background:#007bff;color:white;padding:10px 20px;text-decoration:none;border-radius:5px;'>
            Reset Password
        </a></p>
        <p><small>Link expires in 1 hour.</small></p>
        <p>If you didn't request this, ignore this email.</p>
        <hr>
        <small>Guest House Booking System</small>";

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

            await client.SendMailAsync(message);  // YE SAHI HAI
        }
    }
}
