using System.Collections.Immutable;
using System.Text.Json;
using Forma.Core;
using Xunit;

namespace Forma.Core.Tests;

public class TrackerTests
{
    private static readonly DateOnly Monday = new(2026, 10, 26);
    private static readonly DateOnly Friday = Monday.AddDays(4);
    private static readonly Weekdays Mwf = Weekdays.Monday | Weekdays.Wednesday | Weekdays.Friday;

    private static Habit Check(string name = "Walk", Weekdays days = Weekdays.All, DateOnly? start = null) =>
        new(Guid.NewGuid(), name, HabitKind.Check, 1, "", days, start ?? Monday);
    private static Habit Quantity(string name = "Read", int target = 20) =>
        new(Guid.NewGuid(), name, HabitKind.Quantity, target, "pages", Weekdays.All, Monday, "book", "blue");

    private static TrackerState Accepted(Result<TrackerState> result)
    {
        Assert.True(result.IsSuccess, result.Error);
        return Assert.IsType<TrackerState>(result.Value);
    }

    private static void Rejected(Result<TrackerState> result)
    {
        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }

    private static TrackerState With(params Habit[] habits) => habits.Aggregate(
        TrackerState.Empty, (state, habit) => Accepted(Tracker.SaveHabit(state, habit, Friday)));

    private static TrackerState Log(TrackerState state, Habit habit, DateOnly date, int value, DateOnly? today = null) =>
        Accepted(Tracker.SetProgress(state, habit.Id, date, value, today ?? Friday));

    [Fact]
    public void ScheduleRequiresDayOfWeekAndStartDate()
    {
        var habit = Check(days: Mwf, start: Monday.AddDays(2));

        Assert.False(Tracker.IsScheduled(habit, Monday));
        Assert.True(Tracker.IsScheduled(habit, Monday.AddDays(2)));
        Assert.False(Tracker.IsScheduled(habit, Monday.AddDays(3)));
        Assert.True(Tracker.IsScheduled(habit, Friday));
    }

    [Fact]
    public void MondayBasedWeekStartHandlesYearBoundary()
    {
        Assert.Equal(new DateOnly(2025, 12, 29), Tracker.WeekStart(new DateOnly(2026, 1, 4)));
        Assert.Equal(Monday, Tracker.WeekStart(Friday));
        Assert.Equal(Monday, Tracker.WeekStart(Monday));
    }

    [Fact]
    public void CreateEditAndLogLeavePriorSnapshotsUnchanged()
    {
        var habit = Check();
        var created = With(habit);
        var logged = Log(created, habit, Friday, 1);
        var edited = Accepted(Tracker.SaveHabit(logged, habit with { Name = "Walk home" }, Friday));

        Assert.Empty(TrackerState.Empty.Habits);
        Assert.Equal(habit, Assert.Single(created.Habits));
        Assert.Equal(0, Tracker.GetProgress(created, habit.Id, Friday));
        Assert.Equal("Walk", Assert.Single(logged.Habits).Name);
        Assert.Equal("Walk home", Assert.Single(edited.Habits).Name);
        Assert.Equal(1, Tracker.GetProgress(edited, habit.Id, Friday));
    }

    [Fact]
    public void DeletingOneHabitLeavesTheOtherHabitAndItsLogs()
    {
        var walking = Check();
        var reading = Quantity();
        var state = Log(Log(With(walking, reading), walking, Friday, 1), reading, Friday, 8);

        var next = Accepted(Tracker.DeleteHabit(state, walking.Id));

        Assert.Equal(reading, Assert.Single(next.Habits));
        Assert.False(next.Logs.ContainsKey(walking.Id));
        Assert.Equal(8, Tracker.GetProgress(next, reading.Id, Friday));
        Assert.Equal(1, Tracker.GetProgress(state, walking.Id, Friday));
    }

    [Fact]
    public void ChangingScheduleDropsOnlyEntriesThatNoLongerApply()
    {
        var habit = Check();
        var state = Log(Log(With(habit), habit, Monday, 1), habit, Monday.AddDays(1), 1);

        var next = Accepted(Tracker.SaveHabit(state, habit with { Days = Mwf }, Friday));

        Assert.Equal(1, Tracker.GetProgress(next, habit.Id, Monday));
        Assert.Equal(0, Tracker.GetProgress(next, habit.Id, Monday.AddDays(1)));
        Assert.Equal(1, Tracker.GetProgress(state, habit.Id, Monday.AddDays(1)));
    }

    [Fact]
    public void ChangingStartDateDropsPriorEntries()
    {
        var habit = Check();
        var state = Log(Log(With(habit), habit, Monday, 1), habit, Friday, 1);

        var next = Accepted(Tracker.SaveHabit(state, habit with { StartDate = Monday.AddDays(1) }, Friday));

        Assert.Equal(0, Tracker.GetProgress(next, habit.Id, Monday));
        Assert.Equal(1, Tracker.GetProgress(next, habit.Id, Friday));
    }

    [Fact]
    public void ChangingTrackingKindClearsOnlyThatHabitsEntries()
    {
        var walk = Check();
        var read = Quantity();
        var state = Log(Log(With(walk, read), walk, Friday, 1), read, Friday, 8);
        var edited = walk with { Kind = HabitKind.Quantity, Target = 2, Unit = "km" };

        var next = Accepted(Tracker.SaveHabit(state, edited, Friday));

        Assert.Equal(0, Tracker.GetProgress(next, walk.Id, Friday));
        Assert.Equal(8, Tracker.GetProgress(next, read.Id, Friday));
    }

    [Fact]
    public void QuantityCompletionUsesTargetNotThePresenceOfAnEntry()
    {
        var habit = Quantity();
        var state = Log(With(habit), habit, Monday, 19);

        Assert.False(Tracker.IsComplete(state, habit, Monday));
        var reached = Log(state, habit, Monday, 20);
        Assert.True(Tracker.IsComplete(reached, habit, Monday));
        var cleared = Log(reached, habit, Monday, 0);
        Assert.False(Tracker.IsComplete(cleared, habit, Monday));
        Assert.Equal(0, Tracker.GetProgress(cleared, habit.Id, Monday));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void CheckHabitsRejectValuesOtherThanZeroAndOne(int value)
    {
        var habit = Check();
        Rejected(Tracker.SetProgress(With(habit), habit.Id, Friday, value, Friday));
    }

    [Fact]
    public void SetProgressRejectsRestDaysPastBeforeStartFutureAndUnknownHabit()
    {
        var habit = Check(days: Mwf);
        var state = With(habit);

        Rejected(Tracker.SetProgress(state, habit.Id, Monday.AddDays(1), 1, Friday));
        Rejected(Tracker.SetProgress(state, habit.Id, Monday.AddDays(-3), 1, Friday));
        Rejected(Tracker.SetProgress(state, habit.Id, Monday.AddDays(7), 1, Friday));
        Rejected(Tracker.SetProgress(state, Guid.NewGuid(), Friday, 1, Friday));
        Assert.Empty(state.Logs);
    }

    [Fact]
    public void InvalidHabitDraftsAreRejected()
    {
        var habit = Check();
        var bad = new[]
        {
            habit with { Id = Guid.Empty },
            habit with { Name = "  " },
            habit with { Days = Weekdays.None },
            habit with { Days = (Weekdays)128 },
            habit with { Kind = (HabitKind)99 },
            Quantity(target: 0)
        };
        foreach (var draft in bad)
            Rejected(Tracker.SaveHabit(TrackerState.Empty, draft, Friday));
    }

    [Fact]
    public void CurrentAndBestStreakIgnoreRestDaysButNotMissedScheduledDays()
    {
        var habit = Check(days: Mwf);
        var state = Log(Log(With(habit), habit, Monday, 1), habit, Monday.AddDays(2), 1);

        Assert.Equal(new StreakSummary(2, 2), Tracker.Streaks(state, habit, Monday.AddDays(3)));
        Assert.Equal(new StreakSummary(2, 2), Tracker.Streaks(state, habit, Friday));
        Assert.Equal(new StreakSummary(0, 2), Tracker.Streaks(state, habit, Friday.AddDays(1)));

        var nextMonday = Monday.AddDays(7);
        var next = Log(state, habit, nextMonday, 1, nextMonday);
        Assert.Equal(new StreakSummary(1, 2), Tracker.Streaks(next, habit, nextMonday));
    }

    [Fact]
    public void WeeklySummaryUsesScheduledOpportunitiesNotRawLogCount()
    {
        var walk = Check(days: Mwf, start: Monday.AddDays(2));
        var read = Quantity();
        var state = Log(Log(Log(With(walk, read), walk, Friday, 1), read, Monday, 20), read, Friday, 19);

        var summary = Tracker.Summarize(state, Monday, Friday);

        Assert.Equal(7, summary.Scheduled);
        Assert.Equal(2, summary.Completed);
        Assert.Equal(29, summary.Percent);
    }

    [Fact]
    public void SummaryOfEmptyAndReverseRangesIsZero()
    {
        var empty = Tracker.Summarize(TrackerState.Empty, Monday, Friday);
        var reversed = Tracker.Summarize(With(Check()), Friday, Monday);

        Assert.Equal(0, empty.Percent);
        Assert.Equal(new PeriodSummary(0, 0), reversed);
    }

    [Fact]
    public void BulkPeriodSummaryMatchesADayByDayCount()
    {
        var walk = Check(days: Mwf);
        var read = Quantity();
        var today = Monday.AddDays(59);
        var state = With(walk, read);
        for (var i = 0; i < 60; i++)
        {
            var day = Monday.AddDays(i);
            if (i % 4 != 0 && Tracker.IsScheduled(walk, day))
                state = Log(state, walk, day, 1, today);
            state = Log(state, read, day, i % 3 == 0 ? 20 : 5, today);
        }

        // Covers short, complete-week, and multi-week ranges against a clear reference.
        foreach (var (first, length) in new[] { (-4, 4), (-3, 12), (0, 57), (13, 7), (30, 8), (58, 2) })
        {
            var since = Monday.AddDays(first);
            var through = since.AddDays(length - 1);
            var pairs = from offset in Enumerable.Range(0, length)
                        let date = since.AddDays(offset)
                        from habit in state.Habits
                        where Tracker.IsScheduled(habit, date)
                        select new { Habit = habit, Date = date };
            var expected = new PeriodSummary(
                pairs.Count(x => Tracker.IsComplete(state, x.Habit, x.Date)),
                pairs.Count());

            Assert.Equal(expected, Tracker.Summarize(state, since, through));
        }
    }

    [Fact]
    public void QuantityProgressRejectsNegativeAndOverLimitValues()
    {
        var habit = Quantity();
        var state = With(habit);

        Rejected(Tracker.SetProgress(state, habit.Id, Friday, -1, Friday));
        Rejected(Tracker.SetProgress(state, habit.Id, Friday, Tracker.MaxQuantity + 1, Friday));
        var updated = Log(state, habit, Friday, Tracker.MaxQuantity);
        Assert.Equal(Tracker.MaxQuantity, Tracker.GetProgress(updated, habit.Id, Friday));
    }

    [Fact]
    public void SaveTrimsTextAndNormalizesBinaryTarget()
    {
        var input = Check("  Walk  ") with { Target = 33, Unit = "steps" };
        var stored = Assert.Single(With(input).Habits);

        Assert.Equal("Walk", stored.Name);
        Assert.Equal("", stored.Unit);
        Assert.Equal(1, stored.Target);
    }

    [Fact]
    public void JsonRoundTripRetainsScheduleProgressAndValidation()
    {
        var habit = Quantity();
        var original = Log(With(habit), habit, Monday.AddDays(2), 15);
        var json = JsonSerializer.Serialize(original);
        var decoded = Accepted(Tracker.Validate(JsonSerializer.Deserialize<TrackerState>(json), Friday));

        Assert.Equal(habit, Assert.Single(decoded.Habits));
        Assert.Equal(15, Tracker.GetProgress(decoded, habit.Id, Monday.AddDays(2)));
        var updated = Log(decoded, habit, Monday.AddDays(2), 20);
        Assert.Equal(1, Tracker.Summarize(updated, Monday, Friday).Completed);
        Assert.Equal(15, Tracker.GetProgress(original, habit.Id, Monday.AddDays(2)));
    }

    [Fact]
    public void ImportRejectsMissingCollectionsUnknownAndDuplicateIdentifiers()
    {
        var habit = Check();
        var state = With(habit);
        Rejected(Tracker.Validate(null, Friday));
        Rejected(Tracker.Validate(state with { Habits = default }, Friday));
        Rejected(Tracker.Validate(state with { Logs = null! }, Friday));
        Rejected(Tracker.Validate(state with { Habits = [habit, habit] }, Friday));
        var rogue = ImmutableDictionary<DateOnly, int>.Empty.Add(Friday, 1);
        Rejected(Tracker.Validate(state with { Logs = state.Logs.Add(Guid.NewGuid(), rogue) }, Friday));
    }

    [Fact]
    public void ImportRejectsOutOfRangeUnscheduledAndFutureCheckInsAtomically()
    {
        var habit = Check(days: Mwf);
        var state = Log(With(habit), habit, Monday, 1);
        var entries = state.Logs[habit.Id];
        var invalid = new[]
        {
            entries.Add(Friday, 2),
            entries.Add(Monday.AddDays(1), 1),
            entries.Add(Monday.AddDays(7), 1)
        };

        foreach (var logs in invalid)
            Rejected(Tracker.Validate(state with { Logs = state.Logs.SetItem(habit.Id, logs) }, Friday));
        Assert.Equal(1, Tracker.GetProgress(state, habit.Id, Monday));
        Assert.Equal(0, Tracker.GetProgress(state, habit.Id, Friday));
    }
}
