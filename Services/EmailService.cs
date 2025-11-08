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
    }
}
