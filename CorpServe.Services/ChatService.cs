using CorpServe.Domain.Contracts;
using CorpServe.Domain.Entities;
using CorpServe.Domain.Entities.ChatModule;
using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Domain.Entities.ProposalModule;
using CorpServe.Services.Abstraction;
using CorpServe.Services.Specifications;
using CorpServe.Shared.CommonResult;
using CorpServe.Shared.DTOs.ChatDTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CorpServe.Services
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ChatService> _logger;

        public ChatService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, ILogger<ChatService> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<Result<IEnumerable<ChatRoomDTO>>> GetUserChatRoomsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Error.Unauthorized("Chat.UserRequired", "User identity is required.");

            var chatRoomRepo = _unitOfWork.GetRepository<ChatRoom, string>();
            var spec = new ChatRoomsByUserSpecification(userId);
            var rooms = (await chatRoomRepo.GetAllAsync(spec)).ToList();
            var participantIds = rooms.SelectMany(r => new[] { r.ClientId, r.VendorId }).Distinct().ToList();
            var profilePics = await UserProfilePictureLookup.GetProfilePictureUrlsAsync(_userManager, participantIds);

            var dtos = rooms.Select(room =>
            {
                var senderType = room.ClientId == userId ? SenderType.Client : SenderType.Vendor;
                var otherSenderType = senderType == SenderType.Client ? SenderType.Vendor : SenderType.Client;

                var activeMessages = room.Messages
                    .Where(m => m.DeletedAt == null)
                    .OrderByDescending(m => m.SentAt)
                    .ToList();

                var lastMsg = activeMessages.FirstOrDefault();
                var unread = activeMessages.Count(m => m.Sender == otherSenderType && !m.IsRead);

                profilePics.TryGetValue(room.ClientId, out var clientPic);
                profilePics.TryGetValue(room.VendorId, out var vendorPic);

                return new ChatRoomDTO
                {
                    Id = room.Id,
                    ClientId = room.ClientId,
                    ClientName = room.Client?.FullName ?? "Client",
                    VendorId = room.VendorId,
                    VendorName = room.Vendor?.FullName ?? "Vendor",
                    ClientProfilePictureUrl = string.IsNullOrWhiteSpace(clientPic) ? null : clientPic,
                    VendorProfilePictureUrl = string.IsNullOrWhiteSpace(vendorPic) ? null : vendorPic,
                    Status = room.Status.ToString(),
                    CreatedAt = room.CreatedAt,
                    LastMessage = lastMsg?.Content,
                    LastMessageTime = lastMsg?.SentAt,
                    UnreadCount = unread
                };
            })
            .OrderByDescending(r => r.LastMessageTime ?? r.CreatedAt)
            .ToList();

            return Result<IEnumerable<ChatRoomDTO>>.Ok(dtos);
        }

        public async Task<Result<IEnumerable<MessageDTO>>> GetChatRoomMessagesAsync(string userId, string chatRoomId, int pageIndex, int pageSize)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Error.Unauthorized("Chat.UserRequired", "User identity is required.");

            if (string.IsNullOrWhiteSpace(chatRoomId))
                return Error.Validation("Chat.RoomRequired", "Chat room ID is required.");

            var chatRoomRepo = _unitOfWork.GetRepository<ChatRoom, string>();
            var room = await chatRoomRepo.GetByIdAsync(chatRoomId);
            if (room is null)
                return Error.NotFound("Chat.RoomNotFound", "Chat room not found.");

            if (room.ClientId != userId && room.VendorId != userId)
                return Error.forbidden("Chat.NotMember", "You are not a member of this chat room.");

            var messageRepo = _unitOfWork.GetRepository<Message, string>();
            var spec = new MessagesByChatRoomSpecification(chatRoomId, pageIndex, pageSize);
            var messages = (await messageRepo.GetAllAsync(spec)).ToList();

            var dtos = messages.Select(MapMessageToDto).Reverse().ToList();
            return Result<IEnumerable<MessageDTO>>.Ok(dtos);
        }

        public async Task<Result<MessageDTO>> SendMessageAsync(string userId, SendMessageDTO dto)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Error.Unauthorized("Chat.UserRequired", "User identity is required.");

            if (string.IsNullOrWhiteSpace(dto.ChatRoomId))
                return Error.Validation("Chat.RoomRequired", "Chat room ID is required.");

            if (string.IsNullOrWhiteSpace(dto.Content) && string.IsNullOrWhiteSpace(dto.MediaUrl))
                return Error.Validation("Chat.ContentRequired", "Message content or media is required.");

            var chatRoomRepo = _unitOfWork.GetRepository<ChatRoom, string>();
            var room = await chatRoomRepo.GetByIdAsync(dto.ChatRoomId);
            if (room is null)
                return Error.NotFound("Chat.RoomNotFound", "Chat room not found.");

            if (room.ClientId != userId && room.VendorId != userId)
                return Error.forbidden("Chat.NotMember", "You are not a member of this chat room.");

            if (room.Status != ChatRoomStatus.Active)
                return Error.Validation("Chat.RoomNotActive", "This chat room is no longer active.");

            var senderType = room.ClientId == userId ? SenderType.Client : SenderType.Vendor;

            var messageType = Enum.IsDefined(typeof(MessageType), dto.Type)
                ? (MessageType)dto.Type
                : MessageType.text;

            var message = new Message
            {
                Content = dto.Content?.Trim() ?? string.Empty,
                Type = messageType,
                Sender = senderType,
                SentAt = DateTime.UtcNow,
                IsRead = false,
                ChatRoomId = dto.ChatRoomId,
                MediaUrl = dto.MediaUrl?.Trim(),
                MediaMimeType = dto.MediaMimeType?.Trim(),
                MediaSizeBytes = dto.MediaSizeBytes
            };

            var messageRepo = _unitOfWork.GetRepository<Message, string>();
            await messageRepo.AddAsync(message);
            await _unitOfWork.SaveChangesAsync();

            return MapMessageToDto(message);
        }

        public async Task<Result<bool>> MarkMessagesAsReadAsync(string userId, string chatRoomId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Error.Unauthorized("Chat.UserRequired", "User identity is required.");

            if (string.IsNullOrWhiteSpace(chatRoomId))
                return Error.Validation("Chat.RoomRequired", "Chat room ID is required.");

            var chatRoomRepo = _unitOfWork.GetRepository<ChatRoom, string>();
            var roomSpec = new ChatRoomsByUserSpecification(userId);
            var rooms = (await chatRoomRepo.GetAllAsync(roomSpec)).ToList();
            var room = rooms.FirstOrDefault(r => r.Id == chatRoomId);

            if (room is null)
                return Error.NotFound("Chat.RoomNotFound", "Chat room not found or you are not a member.");

            var senderType = room.ClientId == userId ? SenderType.Client : SenderType.Vendor;
            var otherSenderType = senderType == SenderType.Client ? SenderType.Vendor : SenderType.Client;

            var messageRepo = _unitOfWork.GetRepository<Message, string>();
            var unreadMessages = room.Messages
                .Where(m => m.Sender == otherSenderType && !m.IsRead && m.DeletedAt == null)
                .ToList();

            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
                messageRepo.Update(msg);
            }

            if (unreadMessages.Count > 0)
                await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<Result<int>> GetUnreadChatCountAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Error.Unauthorized("Chat.UserRequired", "User identity is required.");

            var chatRoomRepo = _unitOfWork.GetRepository<ChatRoom, string>();
            var spec = new ChatRoomsByUserSpecification(userId);
            var rooms = (await chatRoomRepo.GetAllAsync(spec)).ToList();

            var totalUnread = 0;
            foreach (var room in rooms)
            {
                var senderType = room.ClientId == userId ? SenderType.Client : SenderType.Vendor;
                var otherSenderType = senderType == SenderType.Client ? SenderType.Vendor : SenderType.Client;

                totalUnread += room.Messages
                    .Count(m => m.Sender == otherSenderType && !m.IsRead && m.DeletedAt == null);
            }

            return totalUnread;
        }

        public async Task<Result<string>> GetChatRoomIdByRequestAsync(string userId, string requestId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Error.Unauthorized("Chat.UserRequired", "User identity is required.");

            if (string.IsNullOrWhiteSpace(requestId))
                return Error.Validation("Chat.RequestRequired", "Request ID is required.");

            var slaRepo = _unitOfWork.GetRepository<SLAContract, string>();
            var sla = await slaRepo.GetByIdAsync(new SlaContractByRequestIdSpecification(requestId));
            if (sla is null)
                return Error.NotFound("Chat.SlaNotFound", "No active contract found for this request.");

            if (sla.ClientId != userId && sla.VendorId != userId)
                return Error.forbidden("Chat.NotMember", "You are not a party to this request.");

            var chatRoomRepo = _unitOfWork.GetRepository<ChatRoom, string>();
            var chatSpec = new ChatRoomByClientAndVendorSpecification(sla.ClientId, sla.VendorId);
            var chatRoom = await chatRoomRepo.GetByIdAsync(chatSpec);
            if (chatRoom is null)
                return Error.NotFound("Chat.RoomNotFound", "Chat room not found for this request.");

            return chatRoom.Id;
        }

        public async Task<Result<ChatRoomParticipantsDTO>> GetRoomParticipantsAsync(string chatRoomId)
        {
            if (string.IsNullOrWhiteSpace(chatRoomId))
                return Error.Validation("Chat.RoomRequired", "Chat room ID is required.");

            var repo = _unitOfWork.GetRepository<ChatRoom, string>();
            var room = await repo.GetByIdAsync(chatRoomId);
            if (room is null)
                return Error.NotFound("Chat.RoomNotFound", "Chat room not found.");

            return new ChatRoomParticipantsDTO
            {
                ClientId = room.ClientId,
                VendorId = room.VendorId
            };
        }

        private static MessageDTO MapMessageToDto(Message m) => new()
        {
            Id = m.Id,
            ChatRoomId = m.ChatRoomId,
            Content = m.Content,
            Type = m.Type.ToString(),
            Sender = m.Sender.ToString(),
            SentAt = m.SentAt,
            IsRead = m.IsRead,
            MediaUrl = m.MediaUrl,
            MediaMimeType = m.MediaMimeType,
            MediaSizeBytes = m.MediaSizeBytes
        };
    }
}
