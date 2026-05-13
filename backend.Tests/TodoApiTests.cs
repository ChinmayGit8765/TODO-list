using System.Net;
using System.Net.Http.Json;
using backend.Domain;
using backend.Services;
using Microsoft.AspNetCore.Mvc.Testing;

namespace backend.Tests;

public class TodoApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TodoApiTests(WebApplicationFactory<Program> factory)
    {
        // A fresh factory per test class is enough — the store is a singleton inside
        // the factory's host. We don't share state between test methods because each
        // method creates its own todo and operates on its own id.
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_ReturnsSeedItems()
    {
        var client = _factory.CreateClient();

        var items = await client.GetFromJsonAsync<List<TodoItem>>("/api/todos");

        Assert.NotNull(items);
        Assert.True(items!.Count >= TodoSeed.Initial.Count);
    }

    [Fact]
    public async Task Post_CreatesItemAndReturnsLocation()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/todos", new { title = "Integration test todo" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var body = await response.Content.ReadFromJsonAsync<TodoItem>();
        Assert.Equal("Integration test todo", body!.Title);
        Assert.Equal($"/api/todos/{body.Id}", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Post_RejectsEmptyTitleWith400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/todos", new { title = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsItemForKnownIdAnd404ForUnknown()
    {
        var client = _factory.CreateClient();
        var created = await CreateTodo(client, "Round-trip");

        var ok = await client.GetAsync($"/api/todos/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        var notFound = await client.GetAsync($"/api/todos/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task Put_UpdatesFieldsAndPreservesSubTasks()
    {
        var client = _factory.CreateClient();
        var created = await CreateTodo(client, "Plan trip");
        await client.PostAsJsonAsync($"/api/todos/{created.Id}/subtasks", new { title = "Book flights" });

        var due = DateTime.UtcNow.AddDays(7);
        var put = await client.PutAsJsonAsync($"/api/todos/{created.Id}",
            new { title = "Plan vacation", dueAt = due, notes = "details", isComplete = true });

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var updated = await put.Content.ReadFromJsonAsync<TodoItem>();
        Assert.Equal("Plan vacation", updated!.Title);
        Assert.Equal("details", updated.Notes);
        Assert.True(updated.IsComplete);
        Assert.Single(updated.SubTasks);
    }

    [Fact]
    public async Task Delete_ReturnsNoContentThen404OnRetry()
    {
        var client = _factory.CreateClient();
        var created = await CreateTodo(client, "to delete");

        var first = await client.DeleteAsync($"/api/todos/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        var second = await client.DeleteAsync($"/api/todos/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
    }

    [Fact]
    public async Task SubTaskLifecycle_AddToggleDelete()
    {
        var client = _factory.CreateClient();
        var parent = await CreateTodo(client, "Groceries");

        var addResp = await client.PostAsJsonAsync($"/api/todos/{parent.Id}/subtasks", new { title = "Milk" });
        Assert.Equal(HttpStatusCode.OK, addResp.StatusCode);
        var withSub = await addResp.Content.ReadFromJsonAsync<TodoItem>();
        var subId = withSub!.SubTasks[0].Id;

        var toggleResp = await client.PutAsync($"/api/todos/{parent.Id}/subtasks/{subId}/toggle", content: null);
        Assert.Equal(HttpStatusCode.OK, toggleResp.StatusCode);
        var toggled = await toggleResp.Content.ReadFromJsonAsync<TodoItem>();
        Assert.True(toggled!.SubTasks[0].IsComplete);

        var deleteResp = await client.DeleteAsync($"/api/todos/{parent.Id}/subtasks/{subId}");
        Assert.Equal(HttpStatusCode.OK, deleteResp.StatusCode);
        var afterDelete = await deleteResp.Content.ReadFromJsonAsync<TodoItem>();
        Assert.Empty(afterDelete!.SubTasks);
    }

    [Fact]
    public async Task ToggleSubTask_ReturnsNotFoundForUnknownSubTask()
    {
        var client = _factory.CreateClient();
        var parent = await CreateTodo(client, "Groceries");

        var response = await client.PutAsync(
            $"/api/todos/{parent.Id}/subtasks/{Guid.NewGuid()}/toggle", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RouteConstraint_RejectsNonGuidIdAs404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/todos/not-a-guid");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<TodoItem> CreateTodo(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync("/api/todos", new { title });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TodoItem>())!;
    }
}
