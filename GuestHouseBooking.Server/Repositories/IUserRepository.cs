using GuestHouseBookingCore.Models;

namespace GuestHouseBookingCore.Repositories
{
    public interface IUserRepository : IRepository<Users>
    {
        Task<Users> GetByEmailOrUsernameAsync(string input);
        Task<IEnumerable<Users>> GetActiveUsersAsync();
    }
}
