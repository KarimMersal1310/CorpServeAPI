using Microsoft.AspNetCore.Mvc;
using CorpServe.Shared.CommonResult;

namespace ToDoManagementAPI.CustomMiddleWare
{
    public class ExceptionHandlerMiddleWare
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlerMiddleWare> _logger;

        public ExceptionHandlerMiddleWare(RequestDelegate next , ILogger<ExceptionHandlerMiddleWare> logger)
        {
            _next = next;
            _logger = logger;
        }
        public async Task Invoke(HttpContext context)
        {
            try
            {
               await _next.Invoke(context);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Something Went Wrong");
                
                var error = Error.Failure();
                var Problem = new ProblemDetails()
                {
                    Type = error.Type.ToString(),
                    Title = error.Code,
                    Detail = e.Message,
                    Instance = context.Request.Path,
                    Status =  e switch
                    {
                        _ => StatusCodes.Status500InternalServerError
                    },
                    Extensions = 
                    {
                        { "traceId", context.TraceIdentifier }
                    }
                };
                context.Response.StatusCode = Problem.Status.Value;
                await context.Response.WriteAsJsonAsync(Problem);
            }
        }
    }
}
