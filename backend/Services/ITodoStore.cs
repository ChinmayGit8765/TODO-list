using backend.Domain;

namespace backend.Services;

/// <summary>
/// Application-layer abstraction over the todo data store. Controllers depend
/// on this interface, not on <see cref="TodoStore"/>, so the implementation
/// can be swapped (e.g. for a real database) without touching the HTTP layer.
/// </summary>
public interface ITodoStore
{
    IReadOnlyCollection<TodoItem> GetAll();
    TodoItem? Get(Guid id);
    TodoItem Add(string title);
    TodoItem? Update(Guid id, TodoUpdate update);
    bool Delete(Guid id);

    TodoItem? AddSubTask(Guid todoId, string title);
    TodoItem? ToggleSubTask(Guid todoId, Guid subTaskId);
    TodoItem? DeleteSubTask(Guid todoId, Guid subTaskId);
}

/// <summary>
/// Internal update payload used by the store. Mirrors
/// <see cref="backend.Contracts.UpdateTodoRequest"/> but lives in the service
/// layer so the store has no dependency on transport-layer contracts.
/// </summary>
public sealed record TodoUpdate(string Title, DateTime? DueAt, string? Notes, bool IsComplete);
