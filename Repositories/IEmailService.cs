namespace GuestHouseBookingCore.Repositories
{
    public interface IEmailService
    {
        Task SendWelcomeEmail(string toEmail, string username, string tempPassword);
        Task SendBookingStatusEmail(string toEmail, string userName, string status, string room, string bed, DateTime checkIn,
            DateTime checkOut, string adminName, string? reason = null);
        Task SendNewBookingAlertToAdmin(string toEmail, string userName, string guestHouse, string room, string bed, DateTime checkIn, 
            DateTime checkOut, string purpose);
        Task SendPasswordChangedEmail(string toEmail, string userName);
        Task SendPasswordResetEmail(string toEmail, string userName, string resetLink);
    }
}
