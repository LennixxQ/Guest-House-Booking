using GuestHouseBookingCore.DTO;
using GuestHouseBookingCore.Helpers;
using GuestHouseBookingCore.Models;
using GuestHouseBookingCore.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GuestHouseBookingCore.Controllers
{
    [Route("api/admin/guest-houses")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class GuestHouseAdminController : Controller
    {
        private readonly IRepository<GuestHouses> _ghRepo;
        private readonly ApplicationDbContext _context;
        private readonly GetCurrentAdmin _getCurrentAdmin;
        private readonly ILogService _logService;

        public GuestHouseAdminController(
            IRepository<GuestHouses> ghRepo,
            ApplicationDbContext context,
            GetCurrentAdmin getCurrentAdmin,
            ILogService logService)
        {
            _ghRepo = ghRepo;
            _context = context;
            _getCurrentAdmin = getCurrentAdmin;
            _logService = logService;
        }

        // -------------------------------------------------------------------------------------
        // GET ALL
        // -------------------------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            var query = _ghRepo.GetAll().AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(g =>
                    g.GuestHouseName.ToLower().Contains(search) ||
                    g.City.ToLower().Contains(search));
            }

            var total = await query.CountAsync();

            var data = await query
                .OrderBy(g => g.GuestHouseName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(g => new
                {
                    g.GuestHouseId,
                    g.GuestHouseName,
                    g.Address,
                    g.City,
                    g.Contact,
                    g.IsAvailable
                })
                .ToListAsync();

            return Ok(new { data, total });
        }

        // -------------------------------------------------------------------------------------
        // CREATE GUEST HOUSE  + LOG
        // -------------------------------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateGuestHouseDto dto)
        {
            var adminName = await _getCurrentAdmin.GetCurrentAdminNameAsync();

            var gh = new GuestHouses
            {
                GuestHouseName = dto.Name,
                Address = dto.Address,
                City = dto.City,
                Contact = dto.Contact,
                IsAvailable = dto.IsAvailable,
                UserId = 1, // default admin
                CreatedBy = adminName
            };

            await _ghRepo.AddAsync(gh);
            await _ghRepo.SaveAsync();

            // LOG ENTRY
            await _logService.LogGuestHouseChangeAsync(
                action: LogAction.Create,
                detail: $"Guest House CREATED → Name: {dto.Name}, City: {dto.City}, Contact: {dto.Contact}",
                userId: null,
                bookingId: null
            );

            return Ok(new { message = "Guest House created!", id = gh.GuestHouseId });
        }

        // -------------------------------------------------------------------------------------
        // UPDATE  + LOG WITH OLD → NEW COMPARISON
        // -------------------------------------------------------------------------------------
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateGuestHouseDto dto)
        {
            var gh = await _ghRepo.GetByIdAsync(id);
            if (gh == null) return NotFound();

            // OLD VALUES (Before update)
            string oldName = gh.GuestHouseName;
            string oldCity = gh.City;
            string oldContact = gh.Contact;
            string oldAddress = gh.Address;
            bool oldAvailability = gh.IsAvailable;

            // UPDATE
            gh.GuestHouseName = dto.Name;
            gh.Address = dto.Address;
            gh.City = dto.City;
            gh.Contact = dto.Contact;
            gh.IsAvailable = dto.IsAvailable;

            _ghRepo.Update(gh);
            await _ghRepo.SaveAsync();

            // LOG ENTRY — Show exactly what changed
            await _logService.LogGuestHouseChangeAsync(
                action: LogAction.Update,
                detail:
                    $"Guest House UPDATED → " +
                    $"Name [{oldName} → {dto.Name}], " +
                    $"City [{oldCity} → {dto.City}], " +
                    $"Contact [{oldContact} → {dto.Contact}], " +
                    $"Address [{oldAddress} → {dto.Address}], " +
                    $"Availability [{oldAvailability} → {dto.IsAvailable}]",
                userId: null,
                bookingId: null
            );

            return Ok(new { message = "Guest House updated!" });
        }

        // -------------------------------------------------------------------------------------
        // DEACTIVATE (SOFT DELETE)  + LOG
        // -------------------------------------------------------------------------------------
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var gh = await _ghRepo.GetByIdAsync(id);
            if (gh == null) return NotFound();

            var adminName = await _getCurrentAdmin.GetCurrentAdminNameAsync();

            gh.IsAvailable = false;
            gh.DeletedBy = adminName;

            _ghRepo.Update(gh);
            await _ghRepo.SaveAsync();

            // LOG ENTRY
            await _logService.LogGuestHouseChangeAsync(
                action: LogAction.Delete,
                detail: $"Guest House DEACTIVATED → {gh.GuestHouseName} ({gh.City}) by {adminName}",
                userId: null,
                bookingId: null
            );

            return Ok(new { message = "Guest House deactivated!" });
        }
    }
}
