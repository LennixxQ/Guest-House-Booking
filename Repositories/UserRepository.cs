using GuestHouseBookingCore.Models;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1;

namespace GuestHouseBookingCore.Repositories
{
    public class UserRepository : Repository<Users>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context) { }

        public async Task<Users?> GetByEmailOrUsernameAsync(string input)
        {
            return await _dbSet
                .FirstOrDefaultAsync(u => u.Email == input || u.UserName == input);
        }

        public async Task<IEnumerable<Users>> GetActiveUsersAsync()
        {
            return await _dbSet
                .Where(u => !u.IsDeleted)
                .ToListAsync();
        }
    }
}