using Microsoft.AspNetCore.Mvc;

namespace ToDoManagementAPI.Factories
{
    public static class ApiResponseFactory
    {
        public static IActionResult GenerateValidationResponse(ActionContext actionContext)
        {
            var Errors = actionContext.ModelState.Where(X => X.Value!.Errors.Count > 0)
                                      .ToDictionary(X => X.Key , X=> X.Value!.Errors.Select(X => X.ErrorMessage)).ToArray();
            var Problem = new ProblemDetails()
            {
                Title = "Validation Errors",
                Detail = "One Or More Validation Error Occured",
                Status = StatusCodes.Status400BadRequest,
                Extensions = { { "Errors", Errors } }
            };
            return new BadRequestObjectResult(Problem);
        }
    }
}
