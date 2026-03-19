using CorpServe.Domain.Entities.SpecializedCategoryModule;

namespace EventHub.Services.Specifications
{
    public sealed class CategoryOrderByIdSpecification : BaseSpecificactions<Category, string>
    {
        public CategoryOrderByIdSpecification() : base(c => true)
        {
            AddOrderBy(c => c.Id);
        }
    }

    public sealed class CategoryAdminListSpecification : BaseSpecificactions<Category, string>
    {
        public CategoryAdminListSpecification(string? search, int pageSize, int pageIndex)
            : base(c => string.IsNullOrWhiteSpace(search)
                || c.Name.Contains(search)
                || (c.Description != null && c.Description.Contains(search)))
        {
            AddInclude(c => c.VendorCategories);
            AddInclude(c => c.Requests);
            AddOrderByDescending(c => c.Requests.Count);
            ApplyPagination(pageSize, pageIndex);
        }
    }

    public sealed class CategoryAdminListCountSpecification : BaseSpecificactions<Category, string>
    {
        public CategoryAdminListCountSpecification(string? search)
            : base(c => string.IsNullOrWhiteSpace(search)
                || c.Name.Contains(search)
                || (c.Description != null && c.Description.Contains(search)))
        {
        }
    }

    public sealed class CategoryByIdSpecification : BaseSpecificactions<Category, string>
    {
        public CategoryByIdSpecification(string categoryId) : base(c => c.Id == categoryId)
        {
            AddInclude(c => c.VendorCategories);
        }
    }

    public sealed class CategoryAdminMetricsSpecification : BaseSpecificactions<Category, string>
    {
        public CategoryAdminMetricsSpecification(string? search)
            : base(c => string.IsNullOrWhiteSpace(search)
                || c.Name.Contains(search)
                || (c.Description != null && c.Description.Contains(search)))
        {
            AddInclude(c => c.VendorCategories);
            AddInclude(c => c.Requests);
        }
    }

    public sealed class CategoriesByIdsSpecification : BaseSpecificactions<Category, string>
    {
        public CategoriesByIdsSpecification(IEnumerable<string> categoryIds)
            : base(c => categoryIds.Contains(c.Id))
        {
        }
    }
}
