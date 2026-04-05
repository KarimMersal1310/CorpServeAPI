using CorpServe.Domain.Entities.IdentityModule;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace CorpServe.Tests.Helpers
{
    internal static class UserManagerMockHelper
    {
        internal static Mock<UserManager<ApplicationUser>> Create()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
                store.Object,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!);
        }
    }
}
