using Microsoft.AspNetCore.Http;
using Pr1.MinWebService.Errors;
using System.IO;

namespace Pr1.MinWebService.Middlewares;
public sealed class RequestSizeLimitMiddleware
{
    private const long MaxBytes = 20 * 1024; // 20 KB
    private readonly RequestDelegate _next;

    public RequestSizeLimitMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {   
        var contentLength = context.Request.ContentLength;
        if (contentLength.HasValue && contentLength.Value > MaxBytes)
            throw new ValidationException($"Размер запроса не должен превышать {MaxBytes} байт");
        
        context.Request.EnableBuffering(); // Позволяет читать тело запроса несколько раз

        if(!contentLength.HasValue)
        {
            // Если Content-Length не указан, читаем тело запроса и проверяем размер
            using var ms = new MemoryStream();
            await context.Request.Body.CopyToAsync(ms);
            if (ms.Length > MaxBytes)
                throw new ValidationException($"Размер запроса не должен превышать {MaxBytes} байт");
            
            context.Request.Body.Position = 0; // Сбрасываем позицию потока для дальнейшей обработки
        }
        
        await _next(context);
    }
}