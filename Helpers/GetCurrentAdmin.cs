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

        public Task<string> GetCurrentAdminEmailAsync()
        {
            var email = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;
            return Task.FromResult(email ?? "Unknown Email");
        }

        public Task<int?> GetCurrentAdminIdAsync()
        {
            var idClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(idClaim, out var adminId))
                return Task.FromResult<int?>(adminId);

            return Task.FromResult<int?>(null);
        }
    }
}