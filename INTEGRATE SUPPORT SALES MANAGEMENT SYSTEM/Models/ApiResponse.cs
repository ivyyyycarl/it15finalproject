namespace INTEGRATE_SUPPORT_SALES_MANAGEMENT_SYSTEM.Models
{
    public class ApiError
    {
        public string Code { get; set; } = "internal_error";
        public string Message { get; set; } = "An error occurred.";
        public string? TraceId { get; set; }
        public object? Details { get; set; }
    }

    public class ApiErrorResponse
    {
        public bool Success { get; set; } = false;
        public ApiError Error { get; set; } = new();
    }

    public static class ApiErrorFactory
    {
        public static ApiErrorResponse Create(int statusCode, string message, string? traceId, object? details = null)
        {
            return new ApiErrorResponse
            {
                Success = false,
                Error = new ApiError
                {
                    Code = MapCode(statusCode),
                    Message = message,
                    TraceId = traceId,
                    Details = details
                }
            };
        }

        public static string GetDefaultMessage(int statusCode)
        {
            return statusCode switch
            {
                StatusCodes.Status400BadRequest => "The request is invalid.",
                StatusCodes.Status401Unauthorized => "Authentication is required.",
                StatusCodes.Status403Forbidden => "You do not have permission to perform this action.",
                StatusCodes.Status404NotFound => "The requested resource was not found.",
                StatusCodes.Status409Conflict => "The request conflicts with the current state of the resource.",
                StatusCodes.Status422UnprocessableEntity => "The request could not be processed.",
                StatusCodes.Status429TooManyRequests => "Too many requests. Please try again later.",
                _ => "An internal error occurred. Please try again later."
            };
        }

        private static string MapCode(int statusCode)
        {
            return statusCode switch
            {
                StatusCodes.Status400BadRequest => "bad_request",
                StatusCodes.Status401Unauthorized => "unauthorized",
                StatusCodes.Status403Forbidden => "forbidden",
                StatusCodes.Status404NotFound => "not_found",
                StatusCodes.Status409Conflict => "conflict",
                StatusCodes.Status422UnprocessableEntity => "unprocessable_entity",
                StatusCodes.Status429TooManyRequests => "too_many_requests",
                _ => "internal_error"
            };
        }
    }
}
