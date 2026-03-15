using CorpServe.Services.Abstraction;
using CorpServe.Shared.DTOs.CategoryDTOs;
using CorpServe.Shared.QueryParams;
using EventHub.Presentation.Controllers;
using EventHub.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CorpServe.Presentation.Controllers
{
    public class CategoriesController : ApiBaseController
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryLookupDTO>>> GetAll()
        {
            var result = await _categoryService.GetAllCategoriesAsync();
            return HandleResult(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin")]
        public async Task<ActionResult<PaginatedResult<CategoriesDTO>>> GetAllForAdmin([FromQuery] CategoryQuaryParams queryParams)
        {
            var result = await _categoryService.GetAllCategoriesAsync(queryParams);
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("admin/Create-Category")]
        public async Task<ActionResult<CategoriesDTO>> CreateCategory([FromBody] CreateUpdateCategoryDTO request)
        {
            var result = await _categoryService.CreateCategoryAsync(GetUserIdFromToken(), request);
            return HandleResult(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("admin/Update-Category/{categoryId}")]
        public async Task<ActionResult<bool>> UpdateCategory(string categoryId, [FromBody] CreateUpdateCategoryDTO request)
        {
            var result = await _categoryService.UpdateCategoryAsync(categoryId, request);
            return HandleResult(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("admin/Delete-Category/{categoryId}")]
        public async Task<ActionResult<bool>> DeleteCategory(string categoryId)
        {
            var result = await _categoryService.DeleteCategoryAsync(categoryId);
            return HandleResult(result);
        }
    }
}
