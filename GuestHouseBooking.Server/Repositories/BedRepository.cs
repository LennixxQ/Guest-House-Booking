using GuestHouseBookingCore.Models;

namespace GuestHouseBookingCore.Repositories
{
    public class BedRepository : Repository<Beds>, IRepository<Beds>
    {
        public BedRepository(ApplicationDbContext context) : base(context) { }
    }
}
