using CorpServe.Domain.Entities.ProposalModule;
using CorpServe.Domain.Entities.RequestModule;
using CorpServe.Domain.Entities.SpecializedCategoryModule;
using CorpServe.Domain.Entities.NotificationModule;
using CorpServe.Domain.Entities.RatingModule;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using CorpServe.Domain.Entities.PaymentModule;
using CorpServe.Domain.Entities.ChatModule;

namespace CorpServe.Domain.Entities.IdentityModule
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = default!;
        public UserStatus Status { get; set; }
        public DateTime JoinedAt { get; set; }
        public string? SuspensionReason { get; set; }
        public DateTime? SuspendedAt { get; set; }
        public int ConsecutiveDelayedSlaCount { get; set; }
        public DateTime? PaymentOverdueWarnedAt { get; set; }

        #region RelationShips
        #region Vendor - VendorCategory
        public ICollection<VendorCategory> VendorCategories { get; set; } = new List<VendorCategory>();

        #endregion

        #region User - UserPreference
        public UserPreference UserPreference { get; set; } = new();
        #endregion

        #region Admin - Category

        public ICollection<Category> Categories { get; set; } = new List<Category>();

        #endregion

        #region Client - Request (1-M)

        public ICollection<Request> Requests { get; set; } = new List<Request>();

        #endregion

        #region Vendor - Proposal  (1-M)

        public ICollection<Proposal> Proposals { get; set; } = new List<Proposal>();

        #endregion

        #region Vendor - SLAContract (1-M)
        
        public ICollection<SLAContract> VendorSLAContracts { get; set; } = new List<SLAContract>();

        #endregion

        #region Client - SLAContract (1-M)

        public ICollection<SLAContract> ClientSLAContracts { get; set; } = new List<SLAContract>();

        #endregion

        #region User - Notifications (1-M)

        public ICollection<SystemNotification> Notifications { get; set; } = new List<SystemNotification>();

        #endregion

        #region Client - Payment (1-M)

        public ICollection<Payment> ClientPayments { get; set; } = new List<Payment>();

        #endregion

        #region Vendor - Payment (1-M)

        public ICollection<Payment> VendorPayments { get; set; } = new List<Payment>();

        #endregion

        #region Client/Vendor - Ratings (1-M)
        public ICollection<Rating> ClientRatings { get; set; } = new List<Rating>();
        public ICollection<Rating> VendorRatings { get; set; } = new List<Rating>();
        #endregion

        #region Client - ChatRoom (1-M)

        public ICollection<ChatRoom> ClientChatRooms { get; set; } = new List<ChatRoom>();

        #endregion

        #region Vendor - ChatRoom (1-M)

        public ICollection<ChatRoom> VendorChatRooms { get; set; } = new List<ChatRoom>();

        #endregion

        #region User - UserProfile (1-1)
        public UserProfile UserProfile { get; set; } = default!;
        #endregion

        #endregion
    }
}
