# forma-habits
Public Forma habit tracker demo. Compiled website only.
# Forma · A better everyday

Forma is a habit tracker rebuilt as a C# and .NET 10 Blazor WebAssembly project. It retains the dark, orange-accented visual direction of the reference and runs without an application server, account or database.

## Using Forma

1. Choose New habit. Pick weekdays and a start date; for a numeric habit set a goal and unit.
2. On Today, check off a habit or log progress toward a target. Use the date controls or History to inspect a past day.
3. Open Insights to see weekly completion and streaks. Unscheduled days do not break a streak and do not count against completion.
4. My habits is where habits can be edited or removed. Settings lets you back up or restore data. Data lives in the current browser profile, not an account. Export a backup before clearing site data or switching devices.

## Structure

src/Forma.Core/    Immutable domain model, validation and pure habit calculations
src/Forma.Web/     Razor UI, browser storage adapter and shared state
tests/            Automated tests
docs/             Defense slides and assessment notes
Browser event → Razor component → Blazor state container
                                       │
                                       v
                           Pure tracker operation (C#)
                                       │
                           Result + next immutable state
                                       │
                     browser storage ←─┴─→ refreshed Razor UI
No HTTP API or account is needed for the main workflow. The storage adapter is an infrastructure boundary, not where habit or streak rules live. Storage is local to the URL and browser profile; the rewrite does not automatically migrate records from the reference site.
