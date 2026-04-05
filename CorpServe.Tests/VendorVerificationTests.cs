using AutoMapper;
using CorpServe.Domain.Contracts;
using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Services;
using CorpServe.Services.Abstraction;
using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.VendorVerify;
using CorpServe.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CorpServe.Tests
{
    public class VendorVerificationTests
    {
        [Fact]
        public async Task SubmitVerificationAsync_ShouldReturnValidation_WhenMoreThanThreeDocuments()
        {
            var unitOfWork = new Mock<IUnitOfWork>();
            var fileStorage = new Mock<IFileStorageService>();
            var userManager = UserManagerMockHelper.Create();
            var mapper = new Mock<IMapper>();
            var notification = new Mock<INotificationService>();
            var logger = new Mock<ILogger<VendorVerifyService>>();

            var sut = new VendorVerifyService(
                unitOfWork.Object,
                fileStorage.Object,
                userManager.Object,
                mapper.Object,
                notification.Object,
                logger.Object);

            var result = await sut.SubmitVerificationAsync("vendor-1", new VendorVerifyRequestDTO
            {
                OrganizationName = "Org",
                Documents = [new Mock<IFormFile>().Object, new Mock<IFormFile>().Object, new Mock<IFormFile>().Object, new Mock<IFormFile>().Object]
            });

            Assert.True(result.IsFailure);
            Assert.Equal(ErrorType.Validation, result.Errors[0].Type);
            Assert.Equal("VendorVerify.MaxDocuments", result.Errors[0].Code);
        }

        [Fact]
        public async Task RejectVerificationAsync_ShouldReturnValidation_WhenRejectReasonMissing()
        {
            var unitOfWork = new Mock<IUnitOfWork>();
            var userManager = UserManagerMockHelper.Create();
            var email = new Mock<IEmailService>();
            var notification = new Mock<INotificationService>();
            var logger = new Mock<ILogger<AdminVendorService>>();

            var sut = new AdminVendorService(
                unitOfWork.Object,
                userManager.Object,
                email.Object,
                notification.Object,
                logger.Object);

            var result = await sut.RejectVerificationAsync("VER-1", "admin-1", " ");

            Assert.True(result.IsFailure);
            Assert.Equal(ErrorType.Validation, result.Errors[0].Type);
            Assert.Equal("VendorVerify.RejectReasonRequired", result.Errors[0].Code);
        }
    }
}
