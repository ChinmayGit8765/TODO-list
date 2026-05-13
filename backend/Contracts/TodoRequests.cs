namespace backend.Contracts;

/// <summary>
/// HTTP request shapes for the Todos API. Kept separate from the
/// <see cref="backend.Domain.TodoItem"/> entity so clients can never set
/// server-controlled fields (<c>Id</c>, <c>CreatedAt</c>, <c>SubTasks</c>)
/// through these contracts.
/// </summary>
public sealed record CreateTodoRequest(string Title);

public sealed record CreateSubTaskRequest(string Title);

public sealed record UpdateTodoRequest(
    string Title,
    DateTime? DueAt,
    string? Notes,
    bool IsComplete);
