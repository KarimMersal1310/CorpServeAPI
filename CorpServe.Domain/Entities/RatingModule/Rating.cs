using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Domain.Entities.PaymentModule;
using CorpServe.Domain.Entities.RequestModule;

namespace CorpServe.Domain.Entities.RatingModule
{
    public class Rating : BaseEntity<string>
    {
        public int Stars { get; set; }
        public string? Comment { get; set; }
        public bool IsLocked { get; set; }
        public DateTime CreatedAt { get; set; }

        #region RelationShips

        #region Request - Rating
        public string RequestId { get; set; } = default!;
        public Request Request { get; set; } = default!;
        #endregion

        #region Payment - Rating
        public string PaymentId { get; set; } = default!;
        public Payment Payment { get; set; } = default!;
        #endregion

        #region Client - Rating
        public string ClientId { get; set; } = default!;
        public ApplicationUser Client { get; set; } = default!;
        #endregion

        #region Vendor - Rating

        public string VendorId { get; set; } = default!;
        public ApplicationUser Vendor { get; set; } = default!;  
        #endregion 
        #endregion

    }
}
