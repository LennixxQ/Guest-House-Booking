namespace GuestHouseBookingCore.Repositories
{
    public interface IEmailService
    {
        Task SendWelcomeEmail(string toEmail, string username, string tempPassword);
    }
}
