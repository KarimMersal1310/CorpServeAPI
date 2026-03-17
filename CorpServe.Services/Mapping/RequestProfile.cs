using AutoMapper;
using CorpServe.Domain.Entities.AIEstimateModule;
using CorpServe.Domain.Entities.RequestModule;
using CorpServe.Shared.DTOs.AIEstimationDTOs;
using CorpServe.Shared.DTOs.RequestDTOs;

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
        }
    }
}
