using CorpServe.Domain.Contracts;
using CorpServe.Services;
using CorpServe.Services.Abstraction;
using CorpServe.Shared.CommonResult;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CorpServe.Tests
{
    public class NotificationServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IRealtimeNotifier> _realtime = new();
        private readonly Mock<ILogger<NotificationService>> _logger = new();

        private NotificationService CreateService()
            => new(_unitOfWork.Object, _realtime.Object, _logger.Object);

        [Fact]
        public async Task SendNotificationAsync_ShouldReturnValidation_WhenTypeInvalid()
        {
            var sut = CreateService();

            var result = await sut.SendNotificationAsync("user-1", "title", "message", "BadType");

            Assert.True(result.IsFailure);
            Assert.Equal(ErrorType.Validation, result.Errors[0].Type);
            Assert.Equal("Notification.InvalidType", result.Errors[0].Code);
        }

        [Fact]
        public async Task SendNotificationToManyAsync_ShouldReturnValidation_WhenRecipientsEmpty()
        {
            var sut = CreateService();

            var result = await sut.SendNotificationToManyAsync([], "title", "message", "Info");

            Assert.True(result.IsFailure);
            Assert.Equal(ErrorType.Validation, result.Errors[0].Type);
            Assert.Equal("Notification.RecipientsRequired", result.Errors[0].Code);
        }

        [Fact]
        public async Task SendNotificationAsync_ShouldReturnValidation_WhenTitleMissing()
        {
            var sut = CreateService();

            var result = await sut.SendNotificationAsync("user-1", "   ", "message", "Info");

            Assert.True(result.IsFailure);
            Assert.Equal(ErrorType.Validation, result.Errors[0].Type);
            Assert.Equal("Notification.TitleRequired", result.Errors[0].Code);
        }
    }
}
