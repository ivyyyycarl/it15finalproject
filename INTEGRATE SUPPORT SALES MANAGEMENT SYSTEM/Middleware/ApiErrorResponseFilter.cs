using INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Middleware
{
    public class ApiErrorResponseFilter : IAsyncResultFilter
    {
        public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var traceId = context.HttpContext.TraceIdentifier;

            if (context.Result is StatusCodeResult statusCodeResult && statusCodeResult.StatusCode >= 400)
            {
                var message = ApiErrorFactory.GetDefaultMessage(statusCodeResult.StatusCode);
                context.Result = new ObjectResult(ApiErrorFactory.Create(statusCodeResult.StatusCode, message, traceId))
                {
                    StatusCode = statusCodeResult.StatusCode
                };
            }
            else if (context.Result is ObjectResult objectResult)
            {
                var statusCode = objectResult.StatusCode ?? context.HttpContext.Response.StatusCode;
                if (statusCode >= 400 && objectResult.Value is not ApiErrorResponse)
                {
                    var message = ExtractMessage(objectResult.Value) ?? ApiErrorFactory.GetDefaultMessage(statusCode);
                    objectResult.Value = ApiErrorFactory.Create(statusCode, message, traceId);
                    objectResult.StatusCode = statusCode;
                }
            }

            return next();
        }

        private static string? ExtractMessage(object? value)
        {
            if (value == null)
            {
                return null;
            }

            var type = value.GetType();
            var messageProp = type.GetProperty("message") ?? type.GetProperty("Message");
            if (messageProp?.GetValue(value) is string msg && !string.IsNullOrWhiteSpace(msg))
            {
                return msg;
            }

            return null;
        }
    }
}
