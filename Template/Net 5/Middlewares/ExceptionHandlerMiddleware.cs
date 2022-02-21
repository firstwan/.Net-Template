using GDBAPI.Domain.Constants;
using GDBAPI.DtoModels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Mime;
using System.Threading.Tasks;


namespace GDBAPI.Middlewares
{
    public class ExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        //private readonly LogBuilder _logBuilder;

        public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
        {
            _next = next;
            //_logBuilder = new LogBuilder(logger);
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                //_logBuilder.Log(LogLevel.Error, ex.ToString());
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = MediaTypeNames.Application.Json;
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            return context.Response.WriteAsync(new ErrorResponseDto<string>()
            {
                Code = ErrorCodeConstants.APPLICATION_ERROR,
                Message = exception.Message
            }.ToString());
        }
    }
}
