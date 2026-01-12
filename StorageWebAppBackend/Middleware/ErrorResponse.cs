using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace StorageWebAppBackend.Middleware
{
    /// <summary>
    /// Custom error response model
    /// </summary>
    public class ErrorResponse
    {
        public string Message { get; set; }
        public string Code { get; set; }
        public int StatusCode { get; set; }
        public string TraceId { get; set; }
        public object Details { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Custom exception types for better error handling
    /// </summary>
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }

    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }

    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message) : base(message) { }
    }

    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message) { }
    }

    /// <summary>
    /// Global error handling middleware
    /// </summary>
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var traceId = context.TraceIdentifier;
            
            // Log the error
            _logger.LogError(exception, 
                "Error occurred. TraceId: {TraceId}, Path: {Path}", 
                traceId, 
                context.Request.Path);

            // Build error response based on exception type
            var errorResponse = BuildErrorResponse(exception, traceId);

            // Set response properties
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = errorResponse.StatusCode;

            // Serialize and write response
            var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }

        private ErrorResponse BuildErrorResponse(Exception exception, string traceId)
        {
            var response = new ErrorResponse
            {
                TraceId = traceId,
                Timestamp = DateTime.UtcNow
            };

            switch (exception)
            {
                case ValidationException validationEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Code = "VALIDATION_ERROR";
                    response.Message = validationEx.Message;
                    break;

                case UnauthorizedException unauthorizedEx:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response.Code = "UNAUTHORIZED";
                    response.Message = unauthorizedEx.Message;
                    break;

                case NotFoundException notFoundEx:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Code = "NOT_FOUND";
                    response.Message = notFoundEx.Message;
                    break;

                case ConflictException conflictEx:
                    response.StatusCode = (int)HttpStatusCode.Conflict;
                    response.Code = "CONFLICT";
                    response.Message = conflictEx.Message;
                    break;

                case Amazon.S3.AmazonS3Exception s3Ex:
                    response.StatusCode = (int)s3Ex.StatusCode;
                    response.Code = "STORAGE_ERROR";
                    response.Message = "Error accessing cloud storage";
                    response.Details = new { S3ErrorCode = s3Ex.ErrorCode };
                    break;

                case Microsoft.Azure.Cosmos.CosmosException cosmosEx:
                    response.StatusCode = (int)cosmosEx.StatusCode;
                    response.Code = "DATABASE_ERROR";
                    response.Message = "Database operation failed";
                    response.Details = new { CosmosErrorCode = cosmosEx.SubStatusCode };
                    break;

                case TimeoutException timeoutEx:
                    response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    response.Code = "TIMEOUT";
                    response.Message = "Request timed out";
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.Code = "INTERNAL_ERROR";
                    response.Message = "An unexpected error occurred";
                    break;
            }

            // In development, include stack trace
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                response.Details = new 
                { 
                    ExceptionType = exception.GetType().Name,
                    StackTrace = exception.StackTrace,
                    InnerException = exception.InnerException?.Message
                };
            }

            return response;
        }
    }
}