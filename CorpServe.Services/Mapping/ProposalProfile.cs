using AutoMapper;
using CorpServe.Domain.Entities.ProposalModule;
using CorpServe.Shared.DTOs.ProposalDTOs;
using System;
using System.Globalization;

namespace CorpServe.Services.Mapping
{
    public class ProposalProfile : Profile
    {
        public ProposalProfile()
        {
            CreateMap<Proposal, ProposalDTO>()
                .ForMember(dest => dest.RequestTitle, opt => opt.MapFrom(src => src.Request.Title))
                .ForMember(dest => dest.VendorName, opt => opt.MapFrom(src => SanitizeDisplayText(src.Vendor.FullName)))
                .ForMember(dest => dest.ClientName, opt => opt.MapFrom(src => SanitizeDisplayText(src.Request.Client.FullName)))
                .ForMember(dest => dest.ProposalStatus, opt => opt.MapFrom(src => src.ProposalStatus.ToString()))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => ProfileDateTimeHelper.ToTimeAgo(src.CreatedAt)))
                .ForMember(dest => dest.ProposalType, opt => opt.MapFrom(src => src.ProposalType.ToString()));

            CreateMap<SLAContract, SLAContractDTO>()
                .ForMember(dest => dest.RequestTitle, opt => opt.MapFrom(src => src.Request.Title))
                .ForMember(dest => dest.ClientName, opt => opt.MapFrom(src => SanitizeDisplayText(src.Client.FullName)))
                .ForMember(dest => dest.VendorName, opt => opt.MapFrom(src => SanitizeDisplayText(src.Vendor.FullName)))
                .ForMember(dest => dest.SLAStatus, opt => opt.MapFrom(src => src.SLAStatus.ToString()))
                .ForMember(dest => dest.RemainingHours, opt => opt.MapFrom(src => (src.Deadline - DateTime.UtcNow).TotalHours))
                .ForMember(dest => dest.WarningLevel, opt => opt.MapFrom(src => ResolveWarningLevel(src)))
                .ForMember(dest => dest.IsWarning, opt => opt.MapFrom(src => IsWarningState(src)));
        }

        private static bool IsWarningState(SLAContract contract)
        {
            var remainingHours = (contract.Deadline - DateTime.UtcNow).TotalHours;
            return contract.SLAStatus == SLAStatus.Delayed
                || (contract.SLAStatus == SLAStatus.Inprogress && remainingHours <= 72);
        }

        private static string ResolveWarningLevel(SLAContract contract)
        {
            if (contract.SLAStatus == SLAStatus.Completed)
                return "Completed";

            if (contract.SLAStatus == SLAStatus.Delayed)
                return "Delayed";

            var remainingHours = (contract.Deadline - DateTime.UtcNow).TotalHours;
            if (remainingHours <= 24)
                return "Critical";

            if (remainingHours <= 72)
                return "Warning";

            return "Normal";
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
