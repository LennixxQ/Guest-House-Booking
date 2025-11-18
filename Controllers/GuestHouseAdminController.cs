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

        public GuestHouseAdminController(IRepository<GuestHouses> ghRepo, ApplicationDbContext context, GetCurrentAdmin getCurrentAdmin)
        {
            _ghRepo = ghRepo;
            _context = context;
            _getCurrentAdmin = getCurrentAdmin;
        }

        // GET ALL
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
        {
            var query = _ghRepo.GetAll().AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(g => g.GuestHouseName.ToLower().Contains(search) || g.City.Contains(search));
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

        // CREATE
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateGuestHouseDto dto)
        {
            var currentAdminName = await _getCurrentAdmin.GetCurrentAdminNameAsync();
            var gh = new GuestHouses
            {
                GuestHouseName = dto.Name,
                Address = dto.Address,
                City = dto.City,
                Contact = dto.Contact,
                IsAvailable = dto.IsAvailable,
                UserId = 1, // Admin ID (current user)
                CreatedBy = currentAdminName
            };

            await _ghRepo.AddAsync(gh);
            await _ghRepo.SaveAsync();

            return Ok(new { message = "Guest House created!", id = gh.GuestHouseId });
        }

        // UPDATE
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateGuestHouseDto dto)
        {
            var gh = await _ghRepo.GetByIdAsync(id);
            if (gh == null) return NotFound();

            gh.GuestHouseName = dto.Name;
            gh.Address = dto.Address;
            gh.City = dto.City;
            gh.Contact = dto.Contact;
            gh.IsAvailable = dto.IsAvailable;

            _ghRepo.Update(gh);
            await _ghRepo.SaveAsync();

            return Ok(new { message = "Guest House updated!" });
        }

        // DELETE
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var gh = await _ghRepo.GetByIdAsync(id);
            if (gh == null) return NotFound();

            var currentAdminName = await _getCurrentAdmin.GetCurrentAdminNameAsync();

            // Soft Delete
            gh.IsAvailable = false;
            gh.DeletedBy = currentAdminName;
            _ghRepo.Update(gh);
            await _ghRepo.SaveAsync();

            return Ok(new { message = "Guest House deactivated!" });
        }
    }
}
