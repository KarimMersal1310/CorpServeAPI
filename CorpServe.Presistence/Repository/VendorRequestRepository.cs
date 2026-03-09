//using EventHub.Domain.Contracts;
//using EventHub.Domain.Entities.OrganizerRequestModule;
//using EventHub.Presistence.Data.DbContext;
//using Microsoft.EntityFrameworkCore;

//namespace EventHub.Presistence.Repository
//{
//    public class VendorRequestRepository : IVendorRequestRepository
//    {
//        private readonly EventHubDbContext _context;

//        public OrganizerRequestRepository(EventHubDbContext context)
//        {
//            _context = context;
//        }

//        public async Task<OrganizerRequest?> GetByUserIdWithDocumentsAsync(string userId)
//        {
//            return await _context.Set<OrganizerRequest>()
//                .Include(r => r.Documents)
//                .FirstOrDefaultAsync(r => r.UserId == userId);
//        }

//        public async Task<IReadOnlyList<OrganizerRequest>> GetPendingWithDocumentsAsync()
//        {
//            return await _context.Set<OrganizerRequest>()
//                .Include(r => r.Documents)
//                .Where(r => r.Status == RequestStatus.Pending)
//                .OrderByDescending(r => r.SubmittedAt)
//                .ToListAsync();
//        }

//        public async Task<OrganizerRequest?> GetByIdWithDocumentsAsync(int id)
//        {
//            return await _context.Set<OrganizerRequest>()
//                .Include(r => r.Documents)
//                .FirstOrDefaultAsync(r => r.Id == id);
//        }

//        public async Task AddAsync(OrganizerRequest request) => await _context.Set<OrganizerRequest>().AddAsync(request);

//        public void Update(OrganizerRequest request) => _context.Set<OrganizerRequest>().Update(request);
//    }
//}
