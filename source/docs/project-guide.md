# Forma | Project and developer guide

## Aim, objectives and plan

**Aim:** Give a person one place to plan recurring habits and measure progress without treating deliberately unscheduled days as failures.

1. Define schedules, check and numeric progress, and the meaning of a streak.
2. Map the main screens: daily logging, management, history/insights and backup.
3. Keep business rules in a C# core. Connect reusable Blazor components to a single state container.
4. Validate input, preserve data across refresh, and provide a backup/recovery path.
5. Verify the domain, persistence boundary and user flows; build and publish the web demo.

## Technology comparison

| Option | Strength | Trade-off for Forma |
| --- | --- | --- |
| Plain HTML/CSS/JavaScript | Small initial download and simplest static hosting. | The business logic would be JavaScript, rather than the independently testable C# core expected for this project. |
| Blazor Server | Runs C# on a server with less client-side .NET payload. | Needs an ASP.NET host and an active connection for interface events. The daily tracker does not need per-user server sessions. |
| Blazor WebAssembly (used) | Runs Razor and independent C# domain logic in the browser; can be hosted statically. | The first load includes the .NET WebAssembly runtime. The app deliberately does not include server accounts or synchronization. |

Background reference: [ASP.NET Core Blazor hosting models](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models?view=aspnetcore-10.0).

## How it was built

| Stage | Result | Rationale |
| --- | --- | --- |
| Define the scenario | Select days, log progress, review results. | Tracking should reflect the routine the user chose. |
| Establish screen structure | **Today** highlights the selected day and habits; **History** reviews a past day; **Insights** explains trends; **Settings** handles recovery. | Keep daily actions separate from analysis and destructive operations. On narrow layouts, arrange panels in a single column. |
| Implement model and rules | `Forma.Core` owns records, schedule validation and metrics. | Pure operations can be checked independently of the current date or browser. |
| Connect the interface | Razor components call `HabitStore`; `BrowserStorage` implements browser I/O. | A single state boundary keeps screen behavior and storage consistent. |
| Verify and publish | Domain/integration checks, browser scenarios, release asset validation and GitHub Pages. | Record observed evidence separately from intended support. |

### UI layout map

```text
Desktop: navigation | selected date and habit cards | summary/coach
Narrow:  navigation
         selected date
         habit cards
         summary/coach
Editor:  name → schedule → tracker type and goal → start date → save
```

The screen layout above describes the interface structure; the Canva presentation uses screenshots of the actual app as visual evidence. The [published demo](https://nurzhigittt.github.io/forma-habits/) is the current visual reference.

## Follow one event through the code

1. A Razor component raises a typed event callback. `App.razor` connects UI actions to the shared `HabitStore`.
2. A binary or numeric logging action calls `Tracker.SetProgress` with the current immutable snapshot, habit ID, explicit day and browser-local current date.
3. `Tracker` returns a typed result with a new snapshot or a validation error, without touching storage or the current clock.
4. `HabitStore` serializes the accepted snapshot as versioned JSON. It updates its in-memory state only after `BrowserStorage` successfully writes via JS interop.
5. The screen rerenders. Reloading initializes from the serialized snapshot; an unreadable backup is handled separately rather than silently overwritten.

## Boundaries and extension ideas

- No external API or database is needed for the current app.
- The JSON backup is versioned; oversized or malformed data is rejected. Data is stored per browser origin and profile.
- A future multi-user version would need authentication, shared server storage and an explicit synchronization strategy. This release does not claim any of those features.
- The source package contains everything needed to run and rebuild locally. In the prepared repository layout, `source/` contains editable code; the existing Pages workflow continues deploying the compiled website archive.
