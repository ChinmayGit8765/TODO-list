namespace backend.Domain;

internal sealed record SeedItem(string Title, int? DueInDays, IReadOnlyList<string> SubTasks);

/// <summary>
/// Static seed data the in-memory store loads on construction. Kept separate
/// so future seeding strategies (file, environment-specific) can replace this
/// without touching the store itself.
/// </summary>
internal static class TodoSeed
{
    public static IReadOnlyList<SeedItem> Initial { get; } =
    [
        new SeedItem("Buy groceries for dinner", DueInDays: 1,
            ["Milk", "Eggs", "Bread", "Tomatoes"]),
        new SeedItem("Finish homework", DueInDays: 3,
            ["Math problems", "Read chapter 5", "Write essay outline"]),
        new SeedItem("Clean the house", DueInDays: null, []),
        new SeedItem("Call the plumber", DueInDays: 7, []),
        new SeedItem("Schedule a dentist appointment", DueInDays: null, [])
    ];
}
