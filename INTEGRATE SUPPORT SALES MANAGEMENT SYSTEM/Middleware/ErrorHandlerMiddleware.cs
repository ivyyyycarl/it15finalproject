using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using System.ComponentModel.DataAnnotations;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Middleware
{
    public class ErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlerMiddleware> _logger;

        public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Resource not found");
                await WriteErrorResponse(context, StatusCodes.Status404NotFound, ex.Message);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error");
                await WriteErrorResponse(context, StatusCodes.Status400BadRequest, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business logic error");
                await WriteErrorResponse(context, StatusCodes.Status409Conflict, ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access");
                await WriteErrorResponse(context, StatusCodes.Status401Unauthorized, "Unauthorized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                await WriteErrorResponse(context, StatusCodes.Status500InternalServerError, "An internal error occurred. Please try again later.");
            }
        }

        private static async Task WriteErrorResponse(HttpContext context, int statusCode, string message)
        {
            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var response = ApiErrorFactory.Create(statusCode, message, context.TraceIdentifier);
            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
