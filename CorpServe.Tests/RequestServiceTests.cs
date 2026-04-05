using AutoMapper;
using CorpServe.Domain.Contracts;
using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Services;
using CorpServe.Services.Abstraction;
using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.RequestDTOs;
using CorpServe.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CorpServe.Tests
{
    public class RequestServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IFileStorageService> _fileStorage = new();
        private readonly Mock<IAIEstimationService> _aiEstimation = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<INotificationService> _notification = new();
        private readonly Mock<ILogger<RequestService>> _logger = new();

        private RequestService CreateService(Mock<UserManager<ApplicationUser>>? userManager = null)
            => new(
                _unitOfWork.Object,
                _fileStorage.Object,
                _aiEstimation.Object,
                _mapper.Object,
                (userManager ?? UserManagerMockHelper.Create()).Object,
                _notification.Object,
                _logger.Object);

        [Fact]
        public async Task CreateRequestAsync_ShouldReturnUnauthorized_WhenClientIdMissing()
        {
            var sut = CreateService();

            var result = await sut.CreateRequestAsync(string.Empty, new CreateRequestDTO());

            Assert.True(result.IsFailure);
            Assert.Equal(ErrorType.Unauthorized, result.Errors[0].Type);
            Assert.Equal("Request.ClientRequired", result.Errors[0].Code);
        }

        [Fact]
        public async Task DeleteRequestAsync_ShouldReturnValidation_WhenRequestIdMissing()
        {
            var sut = CreateService();

            var result = await sut.DeleteRequestAsync("client-1", string.Empty);

            Assert.True(result.IsFailure);
            Assert.Equal(ErrorType.Validation, result.Errors[0].Type);
            Assert.Equal("Request.IdRequired", result.Errors[0].Code);
        }

        [Fact]
        public async Task VendorUpdateRequestProgressAsync_ShouldReturnValidation_WhenPercentageOutOfRange()
        {
            var sut = CreateService();

            var result = await sut.VendorUpdateRequestProgressAsync(
                "vendor-1",
                "REQ-001",
                new UpdateRequestProgressDTO
                {
                    Percentage = 101,
                    Description = "progress"
                });

            Assert.True(result.IsFailure);
            Assert.Equal(ErrorType.Validation, result.Errors[0].Type);
            Assert.Equal("Request.InvalidProgress", result.Errors[0].Code);
        }

        [Fact]
        public async Task VendorUpdateRequestProgressAsync_ShouldReturnValidation_WhenDescriptionMissing()
        {
            var sut = CreateService();

            var result = await sut.VendorUpdateRequestProgressAsync(
                "vendor-1",
                "REQ-001",
                new UpdateRequestProgressDTO
                {
                    Percentage = 50,
                    Description = "   "
                });

            Assert.True(result.IsFailure);
            Assert.Equal(ErrorType.Validation, result.Errors[0].Type);
            Assert.Equal("Request.ProgressDescriptionRequired", result.Errors[0].Code);
        }
    }
}
