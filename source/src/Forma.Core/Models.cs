using System.Collections.Immutable;

namespace Forma.Core;

[Flags]
public enum Weekdays
{
    None = 0,
    Monday = 1 << 0,
    Tuesday = 1 << 1,
    Wednesday = 1 << 2,
    Thursday = 1 << 3,
    Friday = 1 << 4,
    Saturday = 1 << 5,
    Sunday = 1 << 6,
    All = Monday | Tuesday | Wednesday | Thursday | Friday | Saturday | Sunday
}

public enum HabitKind { Check, Quantity }

public sealed record Habit(
    Guid Id,
    string Name,
    HabitKind Kind,
    int Target,
    string Unit,
    Weekdays Days,
    DateOnly StartDate,
    string Icon = "leaf",
    string Color = "sage");

public sealed record TrackerState(
    ImmutableArray<Habit> Habits,
    ImmutableDictionary<Guid, ImmutableDictionary<DateOnly, int>> Logs)
{
    public static TrackerState Empty { get; } = new(
        ImmutableArray<Habit>.Empty,
        ImmutableDictionary<Guid, ImmutableDictionary<DateOnly, int>>.Empty);
}

public sealed record Result<T>(T? Value, string? Error)
{
    public bool IsSuccess => Error is null;
    public static Result<T> Success(T value) => new(value, null);
    public static Result<T> Failure(string error) => new(default, error);
}

public sealed record PeriodSummary(int Completed, int Scheduled)
{
    public int Percent => Scheduled == 0 ? 0 : (int)Math.Round(
        Completed * 100.0 / Scheduled, MidpointRounding.AwayFromZero);
}

public sealed record StreakSummary(int Current, int Best);
