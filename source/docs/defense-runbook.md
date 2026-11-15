# FORMA · IFP final defense

**Target:** 17 minutes of speaking and demonstration, then questions. Open the live website, source repository and project before the call. Keep a local copy with the .NET 10 SDK ready if the network fails. Use sample habit data, not personal records. Both presenters should be ready to explain any part of the code; decide who presents each section without implying unverified implementation roles.

| Time | Slide or screen | Talking points |
| --- | --- | --- |
| 0:00–0:40 | Cover | State both full names. “FORMA is a habit tracker built in C# and Blazor WebAssembly. It tracks the days a person actually planned to do a habit.” |
| 0:40–1:50 | Overview, Why FORMA? | “A normal to-do list can say a task was not completed, but it does not know whether that was a planned rest day. FORMA models schedules and shows progress against those schedules.” |
| 1:50–3:15 | Aim, objectives and plan | “Our aim is to make daily tracking quick and feedback accurate. The user plans days, records a check or a number, and reviews progress. The order of development was rules and state, then interface and storage, then tests and demonstration.” |
| 3:15–4:40 | Habit rules and core features | Walk through a Monday/Wednesday/Friday 10-page goal. A rest day does not add to or break the streak. A missed scheduled day does. A value below the goal is saved but is not marked complete. |
| 4:40–6:50 | Technology comparison | Plain HTML/JavaScript can ship a small frontend but leaves the domain in JavaScript. Blazor Server sends a smaller initial payload but needs a live SignalR connection for interactions. Standalone WASM runs C# in the browser and serves static files; a trade-off is the first runtime download. |
| 6:50–9:15 | Functional core and architecture | “The domain takes immutable state, an explicit date and the requested value. It returns a new state or an error; it never needs the UI, the clock or storage.” Trace one action from a Razor component through HabitStore and Forma.Core, then through BrowserStorage and the UI refresh. |
| 9:15–10:10 | Interface | Show the real product screenshots. Explain the date picker, numeric display and how History complements Today. Demo on a real app screen, not the slide screenshot. |
| 10:10–12:00 | Methodology and QA | State only the checks on the final evidence slide. Explain one unit rule, one state/persistence integration check, and one real browser workflow if executed. Note which browsers or device types were not checked. Do not claim a coverage percentage unless measured. |
| 12:00–16:30 | Live demo | Create “Read” with a 10-page target and include today in the schedule. Record 5 then 10. Show History and Insights. Refresh and check the state persists. Export a JSON backup. If time allows, show the app rejecting a malformed backup without overwriting valid state. |
| 16:30–17:00 | Close and questions | Local data and backup keep the app simple, but nothing syncs automatically across devices. Leave website and repository links visible while answering questions. |

## Five questions to rehearse

1. **What makes the domain functional?** Explicit inputs, pure calculations, and state replacement rather than in-place mutation. The browser storage call is a separate effect.
2. **Why use an immutable state snapshot?** Existing values remain unchanged when an operation rejects input; tests can compare old and new snapshots directly.
3. **What counts as a streak day?** Only a scheduled date with a checkmark or amount meeting the goal. Rest dates do not interrupt the sequence.
4. **Why WebAssembly instead of Blazor Server?** This browser-local workflow doesn't need server sessions or live synchronization. WebAssembly supplies usable C# domain code, at the cost of a larger first download.
5. **Where are habits saved?** In browser storage for that URL and profile. The JSON backup is portable; there is no automatic sync or account.
