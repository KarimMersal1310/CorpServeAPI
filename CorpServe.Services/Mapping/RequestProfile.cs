using AutoMapper;
using CorpServe.Domain.Entities.AIEstimateModule;
using CorpServe.Domain.Entities.RequestModule;
using CorpServe.Shared.DTOs.AIEstimationDTOs;
using CorpServe.Shared.DTOs.RequestDTOs;
using System.Globalization;

namespace CorpServe.Services.Mapping
{
    public class RequestProfile : Profile
    {
        public RequestProfile()
        {
            CreateMap<RequestAttachment, RequestAttachmentDTO>();

            CreateMap<AIEstimation, AIEstimationDTO>();

            CreateMap<Request, RequestDTO>()
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Discription))
                .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CateogryId))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
                .ForMember(dest => dest.ProgressPercentage, opt => opt.MapFrom(src => src.RequestProgress.Percentage))
                .ForMember(dest => dest.RequestStatus, opt => opt.MapFrom(src => src.RequestStatus.ToString()));

            CreateMap<Request, VendorRequestViewDTO>()
                .ForMember(dest => dest.RequestId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.RequestCategory, opt => opt.MapFrom(src => src.Category.Name))
                .ForMember(dest => dest.ClientName, opt => opt.MapFrom(src => SanitizeDisplayText(src.Client.FullName)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Discription))
                .ForMember(dest => dest.BudgetMin, opt => opt.MapFrom(src => src.BudgetMin))
                .ForMember(dest => dest.BudgetMax, opt => opt.MapFrom(src => src.BudgetMax))
                .ForMember(dest => dest.Deadline, opt => opt.MapFrom(src => src.ExpectedDeadline))
                .ForMember(dest => dest.CreatedAt , opt => opt.MapFrom(src => ToTimeAgo(src.CreatedAt)));
        }

        private static string SanitizeDisplayText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var cleaned = new string(value
                .Where(c => char.GetUnicodeCategory(c) != UnicodeCategory.Format)
                .ToArray());

            return cleaned.Trim();
        }
        private static string ToTimeAgo(DateTime date)
        {
            var timeSpan = DateTime.UtcNow - date;
            if (timeSpan.TotalSeconds < 60)
                return $"{timeSpan.Seconds} seconds ago";
            if (timeSpan.TotalMinutes < 60)
                return $"{timeSpan.Minutes} minutes ago";
            if (timeSpan.TotalHours < 24)
                return $"{timeSpan.Hours} hours ago";
            if (timeSpan.TotalDays < 30)
                return $"{timeSpan.Days} days ago";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} months ago";
            return $"{(int)(timeSpan.TotalDays / 365)} years ago";
        }
    }
}
