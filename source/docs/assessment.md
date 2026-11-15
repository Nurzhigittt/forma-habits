# Final project · rubric map

This is a mapping to the supplied C# final-project criteria, not a score or a claim that every target was verified on every platform.

| # | Criterion | Evidence / trade-off |
|---|---|---|
| 1 | Code structure | Framework-independent core, browser UI and test projects have separate responsibilities. |
| 2 | Component reusability | Razor components receive typed parameters and emit callbacks rather than duplicating the tracker logic. |
| 3 | Cross-platform | .NET SDK build and a standards-based web UI. Browser/platform verification is listed below, not assumed. |
| 4 | State | One scoped Blazor state container coordinates immutable snapshots, persistence and rendering. |
| 5–7 | UI, UX, responsiveness | Dark/orange Forma visual hierarchy, date navigation, feedback, labeled form controls and layouts designed for narrow screens. |
| 8 | Core functionality | Create/edit/remove habits, weekday schedules, binary/numeric progress, history and insights. |
| 9, 17 | Advanced / innovation | Schedule-aware streaks and weekly metrics; backup validation rejects invalid state. No cloud or live sync is claimed. |
| 10 | Resilience | Validate changes before applying them; report malformed backups and local storage errors. |
| 11–13 | Efficiency, scalability, resources | Static hosting removes app server load. App data stays local; large collaborative datasets would require a different storage model. First WebAssembly load includes a .NET runtime. |
| 14–15 | Testing and QA | Automated tests and QA notes; distinguish tested flows from suggested follow-ups. |
| 16 | Documentation | README, commands, architecture map and operating instructions. No external API to document. |
| 18 | C#/.NET / Blazor features | Records, immutable collections, DateOnly, flags, typed outcomes, event callbacks and WebAssembly. |
| 19–20 | Professionalism and presentation | Runnable source, release procedure and defense slide deck. |

## Suggested 15–20 minute defense

- 2 min: problem, audience and goal.
- 3 min: why Blazor WebAssembly rather than plain JavaScript or Blazor Server.
- 5 min: create a habit, record progress and explain history/insights.
- 4 min: trace a domain transition and why non-scheduled days do not break a streak.
- 2 min: show tests and a rejected backup, then discuss local storage trade-offs.
- Remaining time: questions.

## QA checklist

- Add a binary habit and a numeric habit with a selected schedule.
- Mark, undo and change progress; reload to check that it persists.
- Check that future dates cannot be logged and unscheduled days do not count.
- Edit and remove a habit; reject a malformed backup without changing the data.
- Check narrow-screen layout, keyboard navigation and focus visibility.
- For cross-browser QA, repeat on Firefox and Safari and test a touch device.

## Design decisions to explain

- **Blazor WebAssembly vs. plain JavaScript:** C# domain logic is independently testable, and reusable Razor components share strongly typed parameters; the price is a larger first download.
- **Blazor WebAssembly vs. Blazor Server:** this app does not need server sessions or SignalR. The tracker is single-user, keeps data in the browser and can be hosted as static content. It does not sync across devices.
- **Historical edits:** changing a habit’s tracking type removes its incompatible previous records. Changing days or the start date can remove records that are no longer valid. Back up first if you want to preserve them.
- **Functional core:** operations do not mutate the caller's snapshot, call the clock or touch storage; the same inputs produce the same result. The UI passes the browser-local date explicitly.

## Verification performed

See the [QA record](QA.md) for passed unit/integration tests, scoped coverage, observed user journeys and checks still outstanding.
