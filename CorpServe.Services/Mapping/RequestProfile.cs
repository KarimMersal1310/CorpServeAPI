using AutoMapper;
using CorpServe.Domain.Entities.AIEstimateModule;
using CorpServe.Domain.Entities.RequestModule;
using CorpServe.Shared.DTOs.AIEstimationDTOs;
using CorpServe.Shared.DTOs.RequestDTOs;
using System.Collections.Generic;
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
                .ForMember(dest => dest.RequestStatus, opt => opt.MapFrom(src => src.RequestStatus.ToString()))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => ProfileDateTimeHelper.ToTimeAgo(src.CreatedAt)))
                .ForMember(dest => dest.RequestAttachments, opt => opt.MapFrom(src => src.RequestAttachments ?? new List<RequestAttachment>()))
                .ForMember(dest => dest.AssignedVendorId, opt => opt.MapFrom(src =>
                    src.SLAContract != null
                        ? src.SLAContract.VendorId
                        : src.RequestProgress != null ? src.RequestProgress.VendorId : null))
                .ForMember(dest => dest.AssignedVendorName, opt => opt.Ignore())
                .ForMember(dest => dest.VendorProfilePictureUrl, opt => opt.Ignore());

            CreateMap<Request, VendorRequestViewDTO>()
                .ForMember(dest => dest.RequestId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.RequestCategory, opt => opt.MapFrom(src => src.Category.Name))
                .ForMember(dest => dest.ClientId, opt => opt.MapFrom(src => src.ClientId))
                .ForMember(dest => dest.ClientName, opt => opt.MapFrom(src => SanitizeDisplayText(src.Client.FullName)))
                .ForMember(dest => dest.ClientProfilePictureUrl, opt => opt.Ignore())
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Discription))
                .ForMember(dest => dest.BudgetMin, opt => opt.MapFrom(src => src.BudgetMin))
                .ForMember(dest => dest.BudgetMax, opt => opt.MapFrom(src => src.BudgetMax))
                .ForMember(dest => dest.Deadline, opt => opt.MapFrom(src => src.ExpectedDeadline))
                .ForMember(dest => dest.CreatedAt , opt => opt.MapFrom(src => ProfileDateTimeHelper.ToTimeAgo(src.CreatedAt)))
                .ForMember(dest => dest.RequestAttachments, opt => opt.MapFrom(src => src.RequestAttachments ?? new List<RequestAttachment>()));
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
    }
}
