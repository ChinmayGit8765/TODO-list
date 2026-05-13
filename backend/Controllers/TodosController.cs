using backend.Contracts;
using backend.Domain;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

/// <summary>
/// Exposes CRUD operations for todos and their embedded subtasks.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class TodosController : ControllerBase
{
    private readonly ITodoStore _store;

    public TodosController(ITodoStore store)
    {
        _store = store;
    }

    /// <summary>Returns all todos, ordered by creation time.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TodoItem>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TodoItem>> GetAll() =>
        Ok(_store.GetAll());

    /// <summary>Returns a single todo, or 404 if no todo has that id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TodoItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TodoItem> GetById(Guid id)
    {
        var item = _store.Get(id);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>Creates a new todo. Title is required and trimmed.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TodoItem), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<TodoItem> Create([FromBody] CreateTodoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { error = "Title is required." });
        }

        var item = _store.Add(request.Title.Trim());
        return Created($"/api/todos/{item.Id}", item);
    }

    /// <summary>Replaces the editable fields on an existing todo.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TodoItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TodoItem> Update(Guid id, [FromBody] UpdateTodoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { error = "Title is required." });
        }

        var update = new TodoUpdate(
            Title: request.Title.Trim(),
            DueAt: request.DueAt,
            Notes: request.Notes,
            IsComplete: request.IsComplete);

        var item = _store.Update(id, update);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>Deletes a todo. Returns 204 on success, 404 if it didn't exist.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(Guid id) =>
        _store.Delete(id) ? NoContent() : NotFound();

    /// <summary>Adds a subtask to the given todo. Returns the updated todo.</summary>
    [HttpPost("{id:guid}/subtasks")]
    [ProducesResponseType(typeof(TodoItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TodoItem> AddSubTask(Guid id, [FromBody] CreateSubTaskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { error = "Title is required." });
        }

        var item = _store.AddSubTask(id, request.Title.Trim());
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>Toggles a subtask's complete flag. Returns the updated todo.</summary>
    [HttpPut("{id:guid}/subtasks/{subId:guid}/toggle")]
    [ProducesResponseType(typeof(TodoItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TodoItem> ToggleSubTask(Guid id, Guid subId)
    {
        var item = _store.ToggleSubTask(id, subId);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>Removes a subtask from a todo. Returns the updated todo.</summary>
    [HttpDelete("{id:guid}/subtasks/{subId:guid}")]
    [ProducesResponseType(typeof(TodoItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TodoItem> DeleteSubTask(Guid id, Guid subId)
    {
        var item = _store.DeleteSubTask(id, subId);
        return item is null ? NotFound() : Ok(item);
    }
}
