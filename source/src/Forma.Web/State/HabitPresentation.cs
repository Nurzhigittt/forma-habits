using Forma.Core;

namespace Forma.Web.State;

public static class HabitPresentation
{
    public static readonly (Weekdays Flag, string Short, string Long)[] Days =
    [
        (Weekdays.Monday, "Mon", "Monday"), (Weekdays.Tuesday, "Tue", "Tuesday"),
        (Weekdays.Wednesday, "Wed", "Wednesday"), (Weekdays.Thursday, "Thu", "Thursday"),
        (Weekdays.Friday, "Fri", "Friday"), (Weekdays.Saturday, "Sat", "Saturday"),
        (Weekdays.Sunday, "Sun", "Sunday")
    ];

    public static readonly (string Name, string Label)[] Icons =
    [
        ("leaf","Leaf"), ("water","Water"), ("book","Book"), ("sun","Sun"),
        ("walk","Walk"), ("moon","Moon"), ("heart","Heart"), ("spark","Spark")
    ];

    public static readonly string[] Colors = ["sage", "blue", "peach", "purple", "yellow"];
    public static string Schedule(Weekdays days) => days == Weekdays.All ? "Every day" :
        string.Join(" · ", Days.Where(d => days.HasFlag(d.Flag)).Select(d => d.Short));
    public static string IconClass(string color) => Colors.Contains(color) ? $"tone-{color}" : "tone-sage";
}
