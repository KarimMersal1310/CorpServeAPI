using CorpServe.Domain.Entities.IdentityModule;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CorpServe.Services
{
    /// <summary>
    /// Batch-loads profile picture URLs for user ids (single query).
    /// </summary>
    public static class UserProfilePictureLookup
    {
        public static async Task<IReadOnlyDictionary<string, string>> GetProfilePictureUrlsAsync(
            UserManager<ApplicationUser> userManager,
            IEnumerable<string> userIds,
            CancellationToken cancellationToken = default)
        {
            var ids = userIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
            if (ids.Count == 0)
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var rows = await userManager.Users
                .AsNoTracking()
                .Where(u => ids.Contains(u.Id))
                .Select(u => new { u.Id, Url = u.UserProfile != null ? u.UserProfile.ProfilePictureUrl : null })
                .ToListAsync(cancellationToken);

            return rows.ToDictionary(x => x.Id, x => x.Url ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        }
    }
}
