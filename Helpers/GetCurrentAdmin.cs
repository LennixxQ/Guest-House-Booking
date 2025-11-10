using GuestHouseBookingCore.Repositories;
using System.Security.Claims;

namespace GuestHouseBookingCore.Helpers
{
    public class GetCurrentAdmin
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserRepository _userRepo;

        public GetCurrentAdmin(IHttpContextAccessor httpContextAccessor, IUserRepository userRepo)
        {
            _httpContextAccessor = httpContextAccessor;
            _userRepo = userRepo;
        }

        public async Task<string> GetCurrentAdminNameAsync()
        {
            var emailClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(emailClaim))
                return "System";

            var user = await _userRepo.GetByEmailOrUsernameAsync(emailClaim);
            return user?.EmpName ?? emailClaim.Split('@')[0];
        }
    }
}