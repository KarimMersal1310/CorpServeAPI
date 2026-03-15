using AutoMapper;
using CorpServe.Domain.Entities.SpecializedCategoryModule;
using CorpServe.Services.Abstraction;
using CorpServe.Shared.DTOs.CategoryDTOs;
using CorpServe.Shared.QueryParams;
using E_Commerce.Shared.CommonResult;
using EventHub.Domain.Contracts;
using EventHub.Shared;
using EventHub.Services.Specifications;

namespace CorpServe.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CategoryService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<IEnumerable<CategoryLookupDTO>>> GetAllCategoriesAsync()
        {
            var categoryRepo = _unitOfWork.GetRepository<Category, string>();
            var specification = new CategoryOrderByIdSpecification();
            var categories = await categoryRepo.GetAllAsync(specification);
            return _mapper.Map<List<CategoryLookupDTO>>(categories);
        }

        public async Task<PaginatedResult<CategoriesDTO>> GetAllCategoriesAsync(CategoryQuaryParams quaryParams)
        {
            var categoryRepo = _unitOfWork.GetRepository<Category, string>();
            var listSpecification = new CategoryAdminListSpecification(quaryParams.Search, quaryParams.PageSize, quaryParams.PageIndex);
            var countSpecification = new CategoryAdminListCountSpecification(quaryParams.Search);

            var categories = await categoryRepo.GetAllAsync(listSpecification);
            var count = await categoryRepo.CountAsync(countSpecification);

            var data = _mapper.Map<List<CategoriesDTO>>(categories);
            return new PaginatedResult<CategoriesDTO>(quaryParams.PageIndex, quaryParams.PageSize, count, data);
        }

        public async Task<Result<CategoriesDTO>> CreateCategoryAsync(string adminId, CreateUpdateCategoryDTO createCategoryDTO)
        {
            if (string.IsNullOrWhiteSpace(adminId))
                return Error.Unauthorized("Category.AdminRequired", "Admin identity is required.");

            var categoryName = createCategoryDTO.CategoryName?.Trim();
            var description = createCategoryDTO.Description?.Trim();
            if (string.IsNullOrWhiteSpace(categoryName))
                return Error.Validation("Category.NameRequired", "Category name is required.");

            if (!string.IsNullOrWhiteSpace(description) && description.Length > 500)
                return Error.Validation("Category.DescriptionTooLong", "Category description cannot exceed 500 characters.");

            var categoryRepo = _unitOfWork.GetRepository<Category, string>();
            var exists = await categoryRepo.AnyAsync(c => c.Name == categoryName);
            if (exists)
                return Error.Failure("Category.AlreadyExists", "Category with the same name already exists.");

            var category = new Category
            {
                Name = categoryName,
                Description = description,
                AdminId = adminId
            };

            await categoryRepo.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<CategoriesDTO>(category);
        }

        public async Task<Result<bool>> UpdateCategoryAsync(string categoryId, CreateUpdateCategoryDTO updateCategoryDTO)
        {
            var categoryName = updateCategoryDTO.CategoryName?.Trim();
            var description = updateCategoryDTO.Description?.Trim();
            if (string.IsNullOrWhiteSpace(categoryName))
                return Error.Validation("Category.NameRequired", "Category name is required.");

            if (!string.IsNullOrWhiteSpace(description) && description.Length > 500)
                return Error.Validation("Category.DescriptionTooLong", "Category description cannot exceed 500 characters.");

            var categoryRepo = _unitOfWork.GetRepository<Category, string>();
            var categorySpecification = new CategoryByIdSpecification(categoryId);
            var category = await categoryRepo.GetByIdAsync(categorySpecification);
            if (category is null)
                return Error.NotFound("Category.NotFound", "Category not found.");

            var duplicateNameExists = await categoryRepo.AnyAsync(c => c.Name == categoryName && c.Id != categoryId);
            if (duplicateNameExists)
                return Error.Failure("Category.AlreadyExists", "Category with the same name already exists.");

            category.Name = categoryName;
            category.Description = description;
            categoryRepo.Update(category);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<Result<bool>> DeleteCategoryAsync(string categoryId)
        {
            var categoryRepo = _unitOfWork.GetRepository<Category, string>();
            var categorySpecification = new CategoryByIdSpecification(categoryId);
            var category = await categoryRepo.GetByIdAsync(categorySpecification);
            if (category is null)
                return Error.NotFound("Category.NotFound", "Category not found.");

            if (category.VendorCategories.Any())
                return Error.Failure("Category.HasVendors", "Cannot delete category with assigned vendors.");

            categoryRepo.Remove(category);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
