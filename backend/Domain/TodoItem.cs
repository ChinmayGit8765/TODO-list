namespace backend.Domain;

/// <summary>
/// Represents a single todo and its embedded subtasks. Treated as an immutable
/// value object — mutations produce a new record via <c>with</c>.
/// </summary>
public sealed record TodoItem(
    Guid Id,
    string Title,
    DateTime CreatedAt,
    DateTime? DueAt,
    string? Notes,
    bool IsComplete,
    IReadOnlyList<SubTask> SubTasks);

public sealed record SubTask(Guid Id, string Title, bool IsComplete);
