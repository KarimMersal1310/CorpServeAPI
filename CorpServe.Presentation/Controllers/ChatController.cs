using CorpServe.Services.Abstraction;
using CorpServe.Shared.DTOs.ChatDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CorpServe.Presentation.Controllers
{
    [Authorize]
    public class ChatController : ApiBaseController
    {
        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/gif", "image/webp",
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "text/plain"
        };

        private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

        private readonly IChatService _chatService;
        private readonly IChatRealtimeNotifier _chatNotifier;
        private readonly IFileStorageService _fileStorage;

        public ChatController(IChatService chatService, IChatRealtimeNotifier chatNotifier, IFileStorageService fileStorage)
        {
            _chatService = chatService;
            _chatNotifier = chatNotifier;
            _fileStorage = fileStorage;
        }

        [HttpGet("rooms")]
        public async Task<ActionResult<IEnumerable<ChatRoomDTO>>> GetRooms()
        {
            var result = await _chatService.GetUserChatRoomsAsync(GetUserIdFromToken());
            return HandleResult(result);
        }

        [HttpGet("rooms/{chatRoomId}/messages")]
        public async Task<ActionResult<IEnumerable<MessageDTO>>> GetMessages(
            string chatRoomId,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 30)
        {
            var result = await _chatService.GetChatRoomMessagesAsync(
                GetUserIdFromToken(), chatRoomId, pageIndex, pageSize);
            return HandleResult(result);
        }

        [HttpPost("rooms/{chatRoomId}/messages")]
        public async Task<ActionResult<MessageDTO>> SendMessage(
            string chatRoomId,
            [FromBody] SendMessageDTO dto)
        {
            dto.ChatRoomId = chatRoomId;
            var result = await _chatService.SendMessageAsync(GetUserIdFromToken(), dto);

            if (result.IsSuccess)
            {
                var participants = await _chatService.GetRoomParticipantsAsync(chatRoomId);
                if (participants.IsSuccess)
                    await _chatNotifier.SendMessageToRoomAsync(chatRoomId, result.Value, participants.Value.ClientId, participants.Value.VendorId);
            }

            return HandleResult(result);
        }

        [HttpPost("rooms/{chatRoomId}/attachment")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<ActionResult<MessageDTO>> SendAttachment(
            string chatRoomId,
            [FromForm] SendAttachmentDTO input)
        {
            var file = input.File;
            if (file is null || file.Length == 0)
                return BadRequest("File is required.");

            if (file.Length > MaxFileSize)
                return BadRequest("File size cannot exceed 10 MB.");

            if (!AllowedMimeTypes.Contains(file.ContentType))
                return BadRequest("File type is not allowed.");

            var mediaUrl = await _fileStorage.UploadAsync(file, "uploads/chat-attachments");

            var isImage = file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
            var dto = new SendMessageDTO
            {
                ChatRoomId = chatRoomId,
                Content = string.IsNullOrWhiteSpace(input.Content) ? file.FileName : input.Content.Trim(),
                Type = isImage ? 2 : 3,
                MediaUrl = mediaUrl,
                MediaMimeType = file.ContentType,
                MediaSizeBytes = file.Length
            };

            var result = await _chatService.SendMessageAsync(GetUserIdFromToken(), dto);

            if (result.IsSuccess)
            {
                var participants = await _chatService.GetRoomParticipantsAsync(chatRoomId);
                if (participants.IsSuccess)
                    await _chatNotifier.SendMessageToRoomAsync(chatRoomId, result.Value, participants.Value.ClientId, participants.Value.VendorId);
            }

            return HandleResult(result);
        }

        [HttpPost("rooms/{chatRoomId}/read")]
        public async Task<ActionResult<bool>> MarkAsRead(string chatRoomId)
        {
            var userId = GetUserIdFromToken();
            var result = await _chatService.MarkMessagesAsReadAsync(userId, chatRoomId);

            if (result.IsSuccess)
            {
                var participants = await _chatService.GetRoomParticipantsAsync(chatRoomId);
                if (participants.IsSuccess)
                {
                    var otherUserId = participants.Value.ClientId == userId
                        ? participants.Value.VendorId
                        : participants.Value.ClientId;
                    await _chatNotifier.NotifyMessagesReadAsync(chatRoomId, userId, otherUserId);
                }
            }

            return HandleResult(result);
        }

        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            var result = await _chatService.GetUnreadChatCountAsync(GetUserIdFromToken());
            return HandleResult(result);
        }

        [HttpGet("room-by-request/{requestId}")]
        public async Task<ActionResult<string>> GetRoomByRequest(string requestId)
        {
            var result = await _chatService.GetChatRoomIdByRequestAsync(
                GetUserIdFromToken(), requestId);
            return HandleResult(result);
        }
    }
}
