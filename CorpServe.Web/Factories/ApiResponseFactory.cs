using Microsoft.AspNetCore.Mvc;
using CorpServe.Shared.CommonResult;

namespace ToDoManagementAPI.Factories
{
    public static class ApiResponseFactory
    {
        public static IActionResult GenerateValidationResponse(ActionContext actionContext)
        {
            var Errors = actionContext.ModelState.Where(X => X.Value!.Errors.Count > 0)
                                      .ToDictionary(X => X.Key , X=> X.Value!.Errors.Select(X => X.ErrorMessage)).ToArray();
            var error = Error.Validation();
            var Problem = new ProblemDetails()
            {
                Type = error.Type.ToString(),
                Title = error.Code,
                Detail = "One Or More Validation Error Occured",
                Status = StatusCodes.Status400BadRequest,
                Extensions = 
                { 
                    { "traceId", actionContext.HttpContext.TraceIdentifier },
                    { "errors", Errors } 
                }
            };
            return new BadRequestObjectResult(Problem);
        }
    }
}
