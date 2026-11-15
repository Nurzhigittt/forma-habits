# FORMA · IFP Final Project

C# / .NET 10 / Blazor WebAssembly habit tracker by **Duiset Nurzhigit and Aldiyar Sakenov**.

- **Website:** https://nurzhigittt.github.io/forma-habits/
- **Editable C# source:** [source/](source/)
- **Run instructions and user guide:** [source/README.md](source/README.md)
- **Architecture and decisions:** [project guide](source/docs/project-guide.md)
- **Test commands:** [testing guide](source/tests/README.md)
- **QA and coverage evidence:** [QA record](source/docs/QA.md)
- **Rubric mapping:** [assessment](source/docs/assessment.md)
- **Defense plan:** [runbook](source/docs/defense-runbook.md)
- **Current automated runs:** https://github.com/Nurzhigittt/forma-habits/actions/workflows/verify.yml

## Features

Create, edit and remove habits; weekday schedules and start dates; check or numeric goals; partial progress; history and insights; schedule-aware streaks; browser persistence; validated JSON backup/restore.

## Run from source

Install the .NET 10 SDK, then:

```sh
cd source
dotnet restore Forma.sln
dotnet run --project src/Forma.Web/Forma.Web.csproj -- --urls http://localhost:5080
dotnet test Forma.sln -c Debug
```

Open http://localhost:5080 while the run command is active.

## Testing

The suite contains **21 unit tests and 11 integration tests**. Integration uses the real HabitStore, BrowserStorage and JSON serializer with a test IJSRuntime. Browser tests cover complete user workflows and responsive layouts; consult the actual Actions run for pass/fail status.

## Deployment

`Forma-Public-Deploy.zip` is the compiled static site. The existing Pages workflow deploys this archive. The verification workflow builds source and checks the public site; it does not silently deploy source changes. The production application source is unchanged by this documentation/test publication.

## Data and scope

Records are local to the browser profile and site origin. There is no account or automatic device sync. Export JSON before changing devices or clearing browser data. Concurrent edits across tabs are not synchronized.

## Assistance and contributions

AI assistance supported implementation, tests, documentation and presentation preparation. Each team member must understand and defend the submitted implementation. Individual contributions should be stated truthfully during defense.

## Using FORMA

1. Choose **New habit**. Pick weekdays and a start date; for a numeric habit set a goal and unit.
2. On **Today**, check off a habit or log progress. Use date controls or **History** to inspect a past day.
3. Open **Insights** to see weekly completion and streaks. Unscheduled days do not break a streak and do not count against completion.
4. Use **My habits** to edit or remove habits. **Settings** exports and restores backups. Export before clearing site data or switching devices.

## State and persistence

Browser events reach Razor callbacks, then HabitStore coordinates a pure C# tracker operation. A successful operation returns a new immutable state. BrowserStorage persists the serialized snapshot and the Razor UI refreshes. No HTTP API or account is needed for the main workflow.
