using System.Collections.Concurrent;
using backend.Domain;

namespace backend.Services;

/// <summary>
/// In-memory implementation of <see cref="ITodoStore"/>. Registered as a
/// singleton so a single instance serves the lifetime of the process.
/// </summary>
/// <remarks>
/// <see cref="ConcurrentDictionary{TKey,TValue}"/> handles single-operation
/// concurrency (gets, puts, removes). The lock serialises compound
/// read-modify-write operations (Update, subtask mutations) so the resulting
/// record is always consistent with the value it derived from.
/// </remarks>
internal sealed class TodoStore : ITodoStore
{
    private readonly ConcurrentDictionary<Guid, TodoItem> _items = new();
    private readonly object _writeLock = new();

    public TodoStore()
    {
        var now = DateTime.UtcNow;
        for (var i = 0; i < TodoSeed.Initial.Count; i++)
        {
            var seed = TodoSeed.Initial[i];
            var subTasks = seed.SubTasks
                .Select(t => new SubTask(Guid.NewGuid(), t, IsComplete: false))
                .ToList();

            var item = new TodoItem(
                Id: Guid.NewGuid(),
                Title: seed.Title,
                CreatedAt: now.AddMilliseconds(i),
                DueAt: seed.DueInDays.HasValue ? now.AddDays(seed.DueInDays.Value) : null,
                Notes: null,
                IsComplete: false,
                SubTasks: subTasks);
            _items[item.Id] = item;
        }
    }

    public IReadOnlyCollection<TodoItem> GetAll() =>
        _items.Values.OrderBy(t => t.CreatedAt).ToList();

    public TodoItem? Get(Guid id) =>
        _items.TryGetValue(id, out var item) ? item : null;

    public TodoItem Add(string title)
    {
        var item = new TodoItem(
            Id: Guid.NewGuid(),
            Title: title,
            CreatedAt: DateTime.UtcNow,
            DueAt: null,
            Notes: null,
            IsComplete: false,
            SubTasks: Array.Empty<SubTask>());
        _items[item.Id] = item;
        return item;
    }

    public TodoItem? Update(Guid id, TodoUpdate update)
    {
        lock (_writeLock)
        {
            if (!_items.TryGetValue(id, out var existing)) return null;
            var updated = existing with
            {
                Title = update.Title,
                DueAt = update.DueAt,
                Notes = update.Notes,
                IsComplete = update.IsComplete
            };
            _items[id] = updated;
            return updated;
        }
    }

    public bool Delete(Guid id) => _items.TryRemove(id, out _);

    public TodoItem? AddSubTask(Guid todoId, string title)
    {
        lock (_writeLock)
        {
            if (!_items.TryGetValue(todoId, out var existing)) return null;
            var newSub = new SubTask(Guid.NewGuid(), title, IsComplete: false);
            var newList = existing.SubTasks.Append(newSub).ToList();
            var updated = existing with { SubTasks = newList };
            _items[todoId] = updated;
            return updated;
        }
    }

    public TodoItem? ToggleSubTask(Guid todoId, Guid subTaskId)
    {
        lock (_writeLock)
        {
            if (!_items.TryGetValue(todoId, out var existing)) return null;
            var index = -1;
            for (var i = 0; i < existing.SubTasks.Count; i++)
            {
                if (existing.SubTasks[i].Id == subTaskId)
                {
                    index = i;
                    break;
                }
            }
            if (index == -1) return null;

            var newList = existing.SubTasks.ToList();
            newList[index] = newList[index] with { IsComplete = !newList[index].IsComplete };
            var updated = existing with { SubTasks = newList };
            _items[todoId] = updated;
            return updated;
        }
    }

    public TodoItem? DeleteSubTask(Guid todoId, Guid subTaskId)
    {
        lock (_writeLock)
        {
            if (!_items.TryGetValue(todoId, out var existing)) return null;
            var newList = existing.SubTasks.Where(s => s.Id != subTaskId).ToList();
            if (newList.Count == existing.SubTasks.Count) return null;
            var updated = existing with { SubTasks = newList };
            _items[todoId] = updated;
            return updated;
        }
    }
}
