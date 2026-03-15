using AutoMapper;
using CorpServe.Domain.Entities.SpecializedCategoryModule;
using CorpServe.Shared.DTOs.CategoryDTOs;

namespace CorpServe.Services.Mapping
{
    public class CategoryProfile : Profile
    {
        public CategoryProfile()
        {
            CreateMap<Category, CategoryLookupDTO>();

            CreateMap<Category, CategoriesDTO>()
                .ForMember(dest => dest.VendorCount, opt => opt.MapFrom(src => src.VendorCategories.Count));
        }
    }
}
