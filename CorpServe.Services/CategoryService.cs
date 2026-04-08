using AutoMapper;
using CorpServe.Domain.Entities.RequestModule;
using CorpServe.Domain.Entities.SpecializedCategoryModule;
using CorpServe.Services.Abstraction;
using CorpServe.Shared.DTOs.CategoryDTOs;
using CorpServe.Shared.QueryParams;
using CorpServe.Shared.CommonResult;
using CorpServe.Domain.Contracts;
using CorpServe.Shared;
using CorpServe.Services.Specifications;
using Microsoft.EntityFrameworkCore;

namespace CorpServe.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICategoryDataQueries _categoryDataQueries;

        public CategoryService(IUnitOfWork unitOfWork, IMapper mapper, ICategoryDataQueries categoryDataQueries)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _categoryDataQueries = categoryDataQueries;
        }

        public async Task<Result<IEnumerable<CategoryLookupDTO>>> GetAllCategoriesAsync()
        {
            var categoryRepo = _unitOfWork.GetRepository<Category, string>();
            var specification = new CategoryOrderByIdSpecification();
            var categories = await categoryRepo.GetAllAsync(specification);
            return _mapper.Map<List<CategoryLookupDTO>>(categories);
        }

        public Task<CategoryAdminManageDTO> GetAllCategoriesAsync(CategoryQuaryParams quaryParams) =>
            _categoryDataQueries.GetAdminManageAsync(quaryParams);

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
                return Error.Conflict("Category.AlreadyExists", "Category with the same name already exists.");

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
                return Error.Conflict("Category.AlreadyExists", "Category with the same name already exists.");

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
                return Error.Conflict("Category.HasVendors", "Cannot delete category with assigned vendors.");

            var requestRepo = _unitOfWork.GetRepository<Request, string>();
            var hasRequests = await requestRepo.AnyAsync(r => r.CateogryId == categoryId);
            if (hasRequests)
                return Error.Conflict("Category.HasRequests", "Cannot delete category with assigned Requests.");

            categoryRepo.Remove(category);
            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Error.Conflict("Category.DeleteFailed", ex.InnerException?.Message ?? ex.Message);
            }

            return true;
        }
    }
}
