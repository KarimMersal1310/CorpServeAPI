using AutoMapper;
using CorpServe.Domain.Contracts;
using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Services;
using CorpServe.Services.Abstraction;
using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.ProposalDTOs;
using CorpServe.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CorpServe.Tests
{
    public class ProposalServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<IEmailService> _email = new();
        private readonly Mock<INotificationService> _notification = new();
        private readonly Mock<ILogger<ProposalService>> _logger = new();

        private ProposalService CreateService(Mock<UserManager<ApplicationUser>>? userManager = null)
            => new(
                _unitOfWork.Object,
                _mapper.Object,
                _email.Object,
                (userManager ?? UserManagerMockHelper.Create()).Object,
                _notification.Object,
                _logger.Object);

        [Fact]
        public async Task ProposalCountForRequestAsync_ShouldReturnUnauthorized_WhenClientMissing()
        {
            var sut = CreateService();

            var result = await sut.ProposalCountForRequestAsync(string.Empty, "REQ-1");

            Assert.True(result.IsFailure);
            Assert.Equal(ErrorType.Unauthorized, result.Errors[0].Type);
            Assert.Equal("Proposal.ClientRequired", result.Errors[0].Code);
        }

        [Fact]
        public async Task VendorAcceptProposalAsync_ShouldReturnUnauthorized_WhenVendorSuspended()
        {
            var userManager = UserManagerMockHelper.Create();
            userManager.Setup(x => x.FindByIdAsync("vendor-1"))
                .ReturnsAsync(new ApplicationUser { Id = "vendor-1", Status = UserStatus.Suspended });

            var sut = CreateService(userManager);

            var result = await sut.VendorAcceptProposalAsync("vendor-1", new CreateProposalDTO
            {
                RequestId = "REQ-1",
                ProposedPrice = 100,
                ProposedDeadline = DateTime.UtcNow.AddDays(1)
            });

            Assert.True(result.IsFailure);
            Assert.Equal(ErrorType.Unauthorized, result.Errors[0].Type);
            Assert.Equal("User.Suspended", result.Errors[0].Code);
        }

        [Fact]
        public async Task GetSlaContractForVendorRequestAsync_ShouldReturnValidation_WhenRequestIdMissing()
        {
            var sut = CreateService();

            var result = await sut.GetSlaContractForVendorRequestAsync("vendor-1", string.Empty);

            Assert.True(result.IsFailure);
            Assert.Equal(ErrorType.Validation, result.Errors[0].Type);
            Assert.Equal("Proposal.RequestRequired", result.Errors[0].Code);
        }
    }
}
