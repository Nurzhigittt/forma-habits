using System.Collections.Immutable;
using System.Numerics;

namespace Forma.Core;

/// <summary>
/// Pure transformations and queries: no clock, storage, or mutable global state.
/// The caller supplies its local calendar date.
/// </summary>
public static class Tracker
{
    public const int MaxHabits = 200;
    public const int MaxQuantity = 1_000_000;
    public const int MaxNameLength = 80;
    public const int MaxUnitLength = 24;
    public const int MaxEntries = 100_000;

    public static Result<TrackerState> SaveHabit(TrackerState state, Habit habit, DateOnly today)
    {
        var normalized = habit with
        {
            Name = habit.Name?.Trim() ?? "",
            Unit = habit.Kind == HabitKind.Check ? "" : habit.Unit?.Trim() ?? "",
            Target = habit.Kind == HabitKind.Check ? 1 : habit.Target,
            Icon = habit.Icon?.Trim() ?? "",
            Color = habit.Color?.Trim() ?? ""
        };

        if (HabitError(normalized, today) is { } error)
            return Result<TrackerState>.Failure(error);

        var original = state.Habits.FirstOrDefault(h => h.Id == normalized.Id);
        if (original is null)
        {
            if (state.Habits.Length >= MaxHabits)
                return Result<TrackerState>.Failure($"A maximum of {MaxHabits} habits can be stored.");
            return Result<TrackerState>.Success(state with { Habits = state.Habits.Add(normalized) });
        }

        // A change of tracking kind changes what every prior value means.
        // Schedule and start-date changes only discard now-invalid entries.
        var logs = state.Logs;
        if (original.Kind != normalized.Kind)
            logs = logs.Remove(normalized.Id);
        else if (logs.TryGetValue(normalized.Id, out var history))
        {
            var compatible = history.RemoveRange(history.Keys.Where(d => !IsScheduled(normalized, d)));
            logs = compatible.IsEmpty ? logs.Remove(normalized.Id) : logs.SetItem(normalized.Id, compatible);
        }

        return Result<TrackerState>.Success(
            new TrackerState(state.Habits.Replace(original, normalized), logs));
    }

    public static Result<TrackerState> DeleteHabit(TrackerState state, Guid id)
    {
        var habit = state.Habits.FirstOrDefault(h => h.Id == id);
        return habit is null
            ? Result<TrackerState>.Failure("This habit no longer exists.")
            : Result<TrackerState>.Success(new TrackerState(state.Habits.Remove(habit), state.Logs.Remove(id)));
    }

    public static Result<TrackerState> SetProgress(TrackerState state, Guid id, DateOnly date, int value, DateOnly today)
    {
        var habit = state.Habits.FirstOrDefault(h => h.Id == id);
        if (habit is null)
            return Result<TrackerState>.Failure("This habit no longer exists.");
        if (date > today || !IsScheduled(habit, date))
            return Result<TrackerState>.Failure("Choose a scheduled day on or before today.");
        if (value < 0 || value > ValueLimit(habit))
            return Result<TrackerState>.Failure($"Enter a whole number from 0 to {ValueLimit(habit):N0}.");

        var history = state.Logs.GetValueOrDefault(id) ?? ImmutableDictionary<DateOnly, int>.Empty;
        if (value != 0 && !history.ContainsKey(date) && state.Logs.Values.Sum(entries => entries.Count) >= MaxEntries)
            return Result<TrackerState>.Failure("The tracker has reached its check-in limit.");
        history = value == 0 ? history.Remove(date) : history.SetItem(date, value);
        var logs = history.IsEmpty ? state.Logs.Remove(id) : state.Logs.SetItem(id, history);
        return Result<TrackerState>.Success(state with { Logs = logs });
    }

    /// <summary>Checks a whole snapshot before it can replace the active state.</summary>
    public static Result<TrackerState> Validate(TrackerState? state, DateOnly today)
    {
        if (state is null || state.Habits.IsDefault || state.Logs is null)
            return Result<TrackerState>.Failure("This data does not contain a valid habit tracker.");
        if (state.Habits.Length > MaxHabits || state.Logs.Count > state.Habits.Length)
            return Result<TrackerState>.Failure("This file contains too many habits or log groups.");

        var habits = new Dictionary<Guid, Habit>(state.Habits.Length);
        foreach (var habit in state.Habits)
        {
            if (habit is null)
                return Result<TrackerState>.Failure("This file contains an empty habit.");
            if (HabitError(habit, today) is { } error)
                return Result<TrackerState>.Failure(error);
            if (!habits.TryAdd(habit.Id, habit))
                return Result<TrackerState>.Failure("This file contains duplicate habit identifiers.");
        }

        var remaining = MaxEntries;
        foreach (var (id, history) in state.Logs)
        {
            if (!habits.TryGetValue(id, out var habit) || history is null)
                return Result<TrackerState>.Failure("This file contains check-ins without a matching habit.");
            if (history.Count > remaining)
                return Result<TrackerState>.Failure("This file contains too many check-ins.");
            remaining -= history.Count;
            foreach (var (date, value) in history)
                if (date > today || !IsScheduled(habit, date) || value <= 0 || value > ValueLimit(habit))
                    return Result<TrackerState>.Failure("This file contains an invalid check-in.");
        }

        return Result<TrackerState>.Success(state);
    }

    public static bool IsScheduled(Habit habit, DateOnly date) =>
        date >= habit.StartDate && (habit.Days & Flag(date.DayOfWeek)) != 0;

    public static int GetProgress(TrackerState state, Guid habitId, DateOnly date) =>
        state.Logs.TryGetValue(habitId, out var history) ? history.GetValueOrDefault(date) : 0;

    public static bool IsComplete(TrackerState state, Habit habit, DateOnly date) =>
        IsScheduled(habit, date) && GetProgress(state, habit.Id, date) >= habit.Target;

    public static DateOnly WeekStart(DateOnly date) => date.AddDays(-MondayIndex(date.DayOfWeek));

    /// <summary>Inclusive date bounds. Whole weeks are counted in bulk.</summary>
    public static PeriodSummary Summarize(TrackerState state, DateOnly from, DateOnly through)
    {
        if (from > through)
            return new PeriodSummary(0, 0);

        var scheduled = 0;
        var completed = 0;
        foreach (var habit in state.Habits)
        {
            var firstDay = from < habit.StartDate ? habit.StartDate : from;
            if (firstDay > through)
                continue;

            var length = through.DayNumber - firstDay.DayNumber + 1;
            scheduled += length / 7 * BitOperations.PopCount((uint)(habit.Days & Weekdays.All));
            var partialDays = length % 7;
            for (var offset = 0; offset < partialDays; offset++)
                if (IsScheduled(habit, firstDay.AddDays(offset)))
                    scheduled++;

            if (!state.Logs.TryGetValue(habit.Id, out var history))
                continue;

            // A weekly chart should not sweep years of check-ins. Conversely,
            // an all-time report with a sparse log should not visit every date.
            if (length < history.Count)
            {
                for (var offset = 0; offset < length; offset++)
                {
                    var date = firstDay.AddDays(offset);
                    if (history.GetValueOrDefault(date) >= habit.Target && IsScheduled(habit, date))
                        completed++;
                }
            }
            else
            {
                completed += history.Count(entry => entry.Key >= firstDay && entry.Key <= through &&
                    entry.Value >= habit.Target && IsScheduled(habit, entry.Key));
            }
        }

        return new PeriodSummary(completed, scheduled);
    }

    /// <summary>
    /// Rest days do not break a streak. An incomplete today is still in progress,
    /// so current streak is evaluated through the last completed/due day.
    /// </summary>
    public static StreakSummary Streaks(TrackerState state, Habit habit, DateOnly today)
    {
        if (!state.Logs.TryGetValue(habit.Id, out var history) || history.IsEmpty)
            return new StreakSummary(0, 0);

        var activeThrough = IsComplete(state, habit, today) ? today : PreviousScheduled(habit, today);
        DateOnly? prior = null;
        var run = 0;
        var current = 0;
        var best = 0;
        var completedDates = history
            .Where(entry => entry.Key <= today && entry.Value >= habit.Target && IsScheduled(habit, entry.Key))
            .Select(entry => entry.Key)
            .Order();

        foreach (var date in completedDates)
        {
            run = prior.HasValue && PreviousScheduled(habit, date) == prior ? run + 1 : 1;
            prior = date;
            best = Math.Max(best, run);
            if (date == activeThrough)
                current = run;
        }

        return new StreakSummary(current, best);
    }

    private static DateOnly? PreviousScheduled(Habit habit, DateOnly before)
    {
        for (var offset = 1; offset <= 7 && before.DayNumber - offset >= habit.StartDate.DayNumber; offset++)
        {
            var date = before.AddDays(-offset);
            if ((habit.Days & Flag(date.DayOfWeek)) != 0)
                return date;
        }
        return null;
    }

    private static int MondayIndex(DayOfWeek day) => ((int)day + 6) % 7;
    private static Weekdays Flag(DayOfWeek day) => (Weekdays)(1 << MondayIndex(day));
    private static int ValueLimit(Habit habit) => habit.Kind == HabitKind.Check ? 1 : MaxQuantity;

    private static bool IsVisualToken(string? token) =>
        token is { Length: > 0 and <= 24 } &&
        token.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    private static string? HabitError(Habit habit, DateOnly today)
    {
        if (habit.Id == Guid.Empty)
            return "A habit needs a valid identifier.";
        if (string.IsNullOrWhiteSpace(habit.Name) || habit.Name.Length > MaxNameLength)
            return $"Enter a habit name with 1 to {MaxNameLength} characters.";
        if (habit.Kind is not (HabitKind.Check or HabitKind.Quantity))
            return "Choose a valid tracking type.";
        if (habit.Target is < 1 or > MaxQuantity || habit.Kind == HabitKind.Check && habit.Target != 1)
            return $"Enter a target from 1 to {MaxQuantity:N0}.";
        if (habit.Unit is null || habit.Unit.Length > MaxUnitLength)
            return $"A unit can contain up to {MaxUnitLength} characters.";
        if (habit.Days == Weekdays.None || (habit.Days & ~Weekdays.All) != 0)
            return "Choose at least one valid day of the week.";
        if (habit.StartDate == default || habit.StartDate > today)
            return "Choose a start date on or before today.";
        if (!IsVisualToken(habit.Icon) || !IsVisualToken(habit.Color))
            return "Choose a valid icon and color.";
        return null;
    }
}
