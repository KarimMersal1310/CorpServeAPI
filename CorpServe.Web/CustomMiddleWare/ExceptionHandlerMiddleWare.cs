using Microsoft.AspNetCore.Mvc;
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
                _logger.LogError("Something Went Wrong");
                
                var Problem = new ProblemDetails()
                {
                    Title = "Error While Processing HTTP Request",
                    Detail = e.Message,
                    Instance = context.Request.Path,
                    Status =  e switch
                    {
                        _ => StatusCodes.Status500InternalServerError
                    },
                };
                context.Response.StatusCode = Problem.Status.Value;
                await context.Response.WriteAsJsonAsync(Problem);
            }
        }
    }
}
