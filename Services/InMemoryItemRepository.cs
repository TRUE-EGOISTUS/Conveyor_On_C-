using System.Collections.Concurrent;
using Pr1.MinWebService.Domain;
using Pr1.MinWebService.Errors;

namespace Pr1.MinWebService.Services;

/// <summary>
/// Простое хранилище в памяти процесса.
/// </summary>
public sealed class InMemoryItemRepository : IItemRepository
{
    private readonly ConcurrentDictionary<Guid, Item> _items = new();
    private readonly ConcurrentDictionary<string, Guid> _nameIndex = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<Item> GetAll()
        => _items.Values
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public Item? GetById(Guid id)
        => _items.TryGetValue(id, out var item) ? item : null;

    public Item Create(string name, decimal price)
    {
        if (!_nameIndex.TryAdd(name, Guid.Empty))
            throw new ValidationException($"Товар с именем '{name}' уже существует.");
        var id = Guid.NewGuid();
        var item = new Item(id, name, price);

        _items[id] = item;
        _nameIndex[name] = id;
        return item;
    }
}
