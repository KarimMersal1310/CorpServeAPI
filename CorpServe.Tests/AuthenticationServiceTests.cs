using CorpServe.Domain.Contracts;
using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Services;
using CorpServe.Services.Abstraction;
using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.AuthDTOs;
using CorpServe.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CorpServe.Tests
{
    public class AuthenticationServiceTests
    {
        [Fact]
        public async Task LoginAsync_ShouldReturnInvalidCredentials_WhenEmailNotFound()
        {
            var userManager = UserManagerMockHelper.Create();
            userManager.Setup(x => x.FindByEmailAsync("no@corp.com")).ReturnsAsync((ApplicationUser?)null);

            var configuration = new ConfigurationBuilder().Build();
            var emailService = new Mock<IEmailService>();
            var notificationService = new Mock<INotificationService>();
            var logger = new Mock<ILogger<AuthenticationService>>();
            var unitOfWork = new Mock<IUnitOfWork>();
            var options = Options.Create(new DataProtectionTokenProviderOptions());

            var sut = new AuthenticationService(
                userManager.Object,
                configuration,
                emailService.Object,
                notificationService.Object,
                logger.Object,
                unitOfWork.Object,
                options);

            var result = await sut.LoginAsync(new LoginDTO
            {
                Email = "no@corp.com",
                Password = "Pass@123"
            });

            Assert.True(result.IsFailure);
            Assert.Equal(ErrorType.InvalidCrendentials, result.Errors[0].Type);
            Assert.Equal("User.InvalidCredentials", result.Errors[0].Code);
        }
    }
}
