using AutoMapper;
using CorpServe.Domain.Entities.VendorVerifyModule;
using CorpServe.Shared.DTOs.VendorVerify;

namespace CorpServe.Services.Mapping
{
    public class VendorVerifyProfile : Profile
    {
        public VendorVerifyProfile()
        {
            CreateMap<VendorCertificate, VendorCertificateDTO>();

            CreateMap<VendorVerify, VendorVerifyDTO>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => (int)src.Status))
                .ForMember(dest => dest.Certificates, opt => opt.MapFrom(src => src.VendorCertificates));
        }
    }
}
