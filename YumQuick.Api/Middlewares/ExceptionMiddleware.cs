using System.Net;
using System.Text.Json;

namespace YumQuick.Api.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // محاولة تنفيذ الطلب بشكل طبيعي
                await _next(context);
            }
            catch (Exception ex)
            {
                // لو حصل خطأ، يتم التقاطه هنا
                _logger.LogError(ex, ex.Message);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                // إعداد شكل الاستجابة (تظهر التفاصيل الدقيقة فقط في بيئة التطوير)
                object response = _env.IsDevelopment()
                    ? new { Message = ex.Message, Details = ex.StackTrace?.ToString() }
                    : new { Message = "An unexpected error occurred. Please try again later." };
                var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                await context.Response.WriteAsync(jsonResponse);
            }
        }
    }
}