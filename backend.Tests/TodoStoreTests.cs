using backend.Domain;
using backend.Services;

namespace backend.Tests;

public class TodoStoreTests
{
    [Fact]
    public void Constructor_SeedsInitialItems()
    {
        var store = new TodoStore();

        var items = store.GetAll();

        Assert.Equal(TodoSeed.Initial.Count, items.Count);
        Assert.Equal(
            TodoSeed.Initial.Select(s => s.Title).ToList(),
            items.Select(i => i.Title).ToList());
    }

    [Fact]
    public void Constructor_SeedsDueDatesAndSubTasksFromSeed()
    {
        var store = new TodoStore();
        var items = store.GetAll().ToList();

        var groceries = items.Single(i => i.Title == "Buy groceries for dinner");
        Assert.NotNull(groceries.DueAt);
        Assert.Equal(4, groceries.SubTasks.Count);
        Assert.Contains(groceries.SubTasks, s => s.Title == "Milk");

        var dentist = items.Single(i => i.Title == "Schedule a dentist appointment");
        Assert.Null(dentist.DueAt);
        Assert.Empty(dentist.SubTasks);
    }

    [Fact]
    public void GetAll_ReturnsItemsOrderedByCreatedAt()
    {
        var store = new TodoStore();
        var ordered = store.GetAll().Select(i => i.CreatedAt).ToList();

        Assert.Equal(ordered.OrderBy(d => d), ordered);
    }

    [Fact]
    public void Add_AppendsItemWithDefaults()
    {
        var store = new TodoStore();

        var added = store.Add("New thing");

        Assert.Equal("New thing", added.Title);
        Assert.Null(added.DueAt);
        Assert.Null(added.Notes);
        Assert.False(added.IsComplete);
        Assert.Empty(added.SubTasks);
        Assert.NotEqual(Guid.Empty, added.Id);
        Assert.Equal(added, store.Get(added.Id));
    }

    [Fact]
    public void Get_ReturnsNullForUnknownId()
    {
        var store = new TodoStore();
        Assert.Null(store.Get(Guid.NewGuid()));
    }

    [Fact]
    public void Update_ReplacesFieldsAndPreservesSubTasks()
    {
        var store = new TodoStore();
        var item = store.Add("Plan trip");
        store.AddSubTask(item.Id, "Book flights");

        var due = DateTime.UtcNow.AddDays(7);
        var updated = store.Update(item.Id, new TodoUpdate("Plan vacation", due, "Flight + hotel", IsComplete: true));

        Assert.NotNull(updated);
        Assert.Equal("Plan vacation", updated!.Title);
        Assert.Equal(due, updated.DueAt);
        Assert.Equal("Flight + hotel", updated.Notes);
        Assert.True(updated.IsComplete);
        Assert.Single(updated.SubTasks);
        Assert.Equal("Book flights", updated.SubTasks[0].Title);
    }

    [Fact]
    public void Update_ReturnsNullForUnknownId()
    {
        var store = new TodoStore();
        Assert.Null(store.Update(Guid.NewGuid(), new TodoUpdate("x", null, null, false)));
    }

    [Fact]
    public void Delete_ReturnsTrueWhenRemovedAndFalseOtherwise()
    {
        var store = new TodoStore();
        var item = store.Add("temp");

        Assert.True(store.Delete(item.Id));
        Assert.False(store.Delete(item.Id));
        Assert.Null(store.Get(item.Id));
    }

    [Fact]
    public void AddSubTask_AppendsToParentAndReturnsUpdatedParent()
    {
        var store = new TodoStore();
        var item = store.Add("Groceries");

        var withSub = store.AddSubTask(item.Id, "Milk");

        Assert.NotNull(withSub);
        Assert.Single(withSub!.SubTasks);
        Assert.Equal("Milk", withSub.SubTasks[0].Title);
        Assert.False(withSub.SubTasks[0].IsComplete);
    }

    [Fact]
    public void AddSubTask_ReturnsNullForUnknownParent()
    {
        var store = new TodoStore();
        Assert.Null(store.AddSubTask(Guid.NewGuid(), "Milk"));
    }

    [Fact]
    public void ToggleSubTask_FlipsCompleteStateOnlyForTarget()
    {
        var store = new TodoStore();
        var item = store.Add("Groceries");
        var withTwo = store.AddSubTask(item.Id, "Milk")!;
        withTwo = store.AddSubTask(item.Id, "Eggs")!;
        var milkId = withTwo.SubTasks[0].Id;
        var eggsId = withTwo.SubTasks[1].Id;

        var toggled = store.ToggleSubTask(item.Id, milkId);

        Assert.NotNull(toggled);
        Assert.True(toggled!.SubTasks.Single(s => s.Id == milkId).IsComplete);
        Assert.False(toggled.SubTasks.Single(s => s.Id == eggsId).IsComplete);

        // Toggling again flips back.
        var doubled = store.ToggleSubTask(item.Id, milkId);
        Assert.False(doubled!.SubTasks.Single(s => s.Id == milkId).IsComplete);
    }

    [Fact]
    public void ToggleSubTask_ReturnsNullForUnknownSubTask()
    {
        var store = new TodoStore();
        var item = store.Add("Groceries");

        Assert.Null(store.ToggleSubTask(item.Id, Guid.NewGuid()));
    }

    [Fact]
    public void DeleteSubTask_RemovesOnlyTargetSubTask()
    {
        var store = new TodoStore();
        var item = store.Add("Groceries");
        store.AddSubTask(item.Id, "Milk");
        var withTwo = store.AddSubTask(item.Id, "Eggs")!;
        var milkId = withTwo.SubTasks[0].Id;

        var afterDelete = store.DeleteSubTask(item.Id, milkId);

        Assert.NotNull(afterDelete);
        Assert.Single(afterDelete!.SubTasks);
        Assert.Equal("Eggs", afterDelete.SubTasks[0].Title);
    }

    [Fact]
    public void DeleteSubTask_ReturnsNullWhenSubTaskNotFound()
    {
        var store = new TodoStore();
        var item = store.Add("Groceries");

        Assert.Null(store.DeleteSubTask(item.Id, Guid.NewGuid()));
    }
}
