using Forma.Core;
using Forma.Web.Components;

namespace Forma.Web.State;

// Cached per immutable snapshot: switching tabs or editing a draft does not recompute analytics.
public sealed record ProgressView(
    PeriodSummary Today, PeriodSummary Week, PeriodSummary Month,
    IReadOnlyDictionary<Guid, StreakSummary> Streaks,
    IReadOnlyList<WeekChart.ChartPoint> WeekChart,
    IReadOnlyList<WeekChart.ChartPoint> WeekdayPattern,
    int BestCurrent, int BestEver, string CoachTitle, string CoachMessage)
{
    public static ProgressView Create(TrackerState state, DateOnly date)
    {
        var monday = Tracker.WeekStart(date);
        var week = Enumerable.Range(0, 7).Select(i => monday.AddDays(i)).Select(d =>
        {
            var count = d > date ? new PeriodSummary(0, 0) : Tracker.Summarize(state, d, d);
            return new WeekChart.ChartPoint(d.ToString("ddd", System.Globalization.CultureInfo.InvariantCulture),
                count.Percent, d > date ? "upcoming" : Detail(count));
        }).ToArray();
        var monthStart = date.AddDays(-27);
        var byDay = Enumerable.Range(0, 28).Select(i => monthStart.AddDays(i))
            .GroupBy(d => d.DayOfWeek)
            .ToDictionary(g => g.Key, g =>
            {
                var summaries = g.Select(d => Tracker.Summarize(state, d, d)).ToArray();
                return new PeriodSummary(summaries.Sum(s => s.Completed), summaries.Sum(s => s.Scheduled));
            });
        var ordered = Enumerable.Range(0, 7).Select(i => (DayOfWeek)((i + 1) % 7)).ToArray();
        var pattern = ordered.Select(day =>
            new WeekChart.ChartPoint(day.ToString()[..3], byDay[day].Percent, Detail(byDay[day]))).ToArray();
        var streaks = state.Habits.ToDictionary(h => h.Id, h => Tracker.Streaks(state, h, date));
        var month = new PeriodSummary(byDay.Values.Sum(s => s.Completed), byDay.Values.Sum(s => s.Scheduled));
        var (title, message) = Recommend(state, month, ordered, byDay);
        return new ProgressView(Tracker.Summarize(state, date, date), Tracker.Summarize(state, monday, date),
            month, streaks, week, pattern,
            streaks.Values.Select(s => s.Current).DefaultIfEmpty().Max(),
            streaks.Values.Select(s => s.Best).DefaultIfEmpty().Max(), title, message);
    }

    private static (string Title, string Message) Recommend(TrackerState state, PeriodSummary month,
        DayOfWeek[] ordered, Dictionary<DayOfWeek, PeriodSummary> pattern)
    {
        if (state.Habits.Length == 0)
            return ("Start with one small thing.", "Pick a repeatable habit. One realistic action is enough for a first week.");
        if (month.Completed < 3)
            return ("Give it a few check-ins.", "Log at least three completed check-ins for a recommendation based on your pattern.");
        var weakest = ordered.Where(d => pattern[d].Scheduled >= 3)
            .OrderBy(d => pattern[d].Completed / (double)pattern[d].Scheduled)
            .Select(d => (DayOfWeek?)d).FirstOrDefault();
        if (weakest is { } day && pattern[day].Percent < 70)
        {
            var p = pattern[day];
            return ($"Make {day} easier.",
                $"Over the last 28 days, you completed {p.Completed} of {p.Scheduled} scheduled {day} check-ins. Try doing one habit right after something already on your calendar.");
        }
        return ("Keep what’s working.", "You have several completed check-ins and no recurring low day with enough observations. Keep your current routine another week.");
    }

    private static string Detail(PeriodSummary counts) =>
        counts.Scheduled == 0 ? "no habits scheduled" : $"{counts.Completed} of {counts.Scheduled} complete";
}
