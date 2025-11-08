using System.Text.RegularExpressions;

namespace GuestHouseBookingCore.Services
{
    public class RegisterService
    {
        public string GenerateUsername(string name)
        {
            var clean = Regex.Replace(name.ToLower(), @"[^a-z]", "");
            var random = Path.GetRandomFileName().Replace(".", "").Substring(0, 4);
            return $"{clean}_{random}";
        }
    }
}
