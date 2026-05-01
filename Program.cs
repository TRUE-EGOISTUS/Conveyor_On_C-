using Microsoft.AspNetCore.Http.Json;
using System.Text.Json.Serialization;
using Pr1.MinWebService.Domain;
using Pr1.MinWebService.Errors;
using Pr1.MinWebService.Middlewares;
using Pr1.MinWebService.Services;

var builder = WebApplication.CreateBuilder(args);

// Настройка сериализации, чтобы ответы были компактнее
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddSingleton<IItemRepository, InMemoryItemRepository>();

var app = builder.Build();

// Конвейер обработки запросов
app.UseMiddleware<RequestIdMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RequestSizeLimitMiddleware>();
app.UseMiddleware<TimingAndLogMiddleware>();


// Точка доступа для чтения списка
app.MapGet("/api/items", (IItemRepository repo, HttpRequest req) =>
{
    var items = repo.GetAll().AsEnumerable();
    
    var name = req.Query["name"].ToString();
    var minPrice = req.Query["minPrice"].ToString();
    var maxPrice = req.Query["maxPrice"].ToString();

    if (!string.IsNullOrWhiteSpace(name))
        items = items.Where(x => x.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

    if (decimal.TryParse(minPrice, out var min))
        items = items.Where(x => x.Price >= min);
    
    if (decimal.TryParse(maxPrice, out var max))
        items = items.Where(x => x.Price <= max);
    

    var sort = req.Query["sort"].ToString();
    var order = req.Query["order"].ToString();

    var desc = string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase);
    items = sort switch
    {
        "price" => desc ? items.OrderByDescending(x => x.Price) : items.OrderBy(x => x.Price),
        _ => desc ? items.OrderByDescending(x => x.Name) : items.OrderBy(x => x.Name),
    };
    
    return Results.Ok(items.ToArray());
});

// Точка доступа для чтения по идентификатору
app.MapGet("/api/items/{id:guid}", (Guid id, IItemRepository repo) =>
{
    var item = repo.GetById(id);
    if (item is null)
        throw new NotFoundException("Элемент не найден");

    return Results.Ok(item);
});

// Точка доступа для создания
app.MapPost("/api/items", (HttpContext ctx, CreateItemRequest request, IItemRepository repo) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
        throw new ValidationException("Поле name не должно быть пустым");

    if (request.Price < 0)
        throw new ValidationException("Поле price не может быть отрицательным");

    if (request.Name.Length > 150)
        throw new ValidationException("Поле name не должно превышать 150 символов");

    var created = repo.Create(request.Name.Trim(), request.Price);

    // Адрес созданного ресурса без привязки к конкретному хосту
    var location = $"/api/items/{created.Id}";
    ctx.Response.Headers.Location = location;

    return Results.Created(location, created);
});

app.Run();

// Нужен для проекта с испытаниями
public partial class Program { }
