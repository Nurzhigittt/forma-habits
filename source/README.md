# Forma | IFP Final Project

Forma is a C# / .NET 10 Blazor WebAssembly habit tracker. It records check-off or numeric progress against a weekly schedule, then shows history, completion and streaks. Rest days do not count as missed days.

**Public demo:** [Open Forma](https://nurzhigittt.github.io/forma-habits/). No account is needed. This archive contains the editable source, tests and documentation. The public demo repository contains the compiled website, not this source package.

## Run the project

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). The runtime alone is not sufficient to build the project.
2. Extract the ZIP. Open Terminal or PowerShell in the extracted `Forma-Blazor` folder, where `Forma.sln` is located.
3. Run:

   ```sh
   dotnet restore Forma.sln
   dotnet run --project src/Forma.Web/Forma.Web.csproj -- --urls http://localhost:5080
   ```

4. Open [localhost:5080](http://localhost:5080). Keep the terminal open while using the local app. Stop it with `Ctrl+C`.

To build for hosting:

```sh
dotnet publish src/Forma.Web/Forma.Web.csproj -c Release
```

Deploy the published `wwwroot` files from a static web server that serves WebAssembly, including all `_framework` files. The relative HTML base path also supports a GitHub Pages project subdirectory. The production demo does not require an ASP.NET server or an external API.

## Use Forma

| Step | What to do |
| --- | --- |
| Create | **New habit**: name it, select weekdays and a start date. Choose a check habit or a numeric target and unit. |
| Log | In **Today**, mark the check habit or add numeric progress. Tracking follows the selected date and schedule. |
| Review | In **History**, inspect a date. In **Insights**, inspect completion and streaks. |
| Manage | In **My habits**, edit or remove habits. Changing the tracking method or schedule may remove incompatible old records; back up before making structural edits. |
| Back up | In **Settings**, export JSON. Import reviews the selected backup before replacement; invalid backups do not overwrite current data. |

Data lives in the current browser's local storage. It does not automatically sync across devices or move from the old ChatGPT-hosted preview to the public website. To transfer records, export on the old site and import on the new one. Avoid putting private records in a public demo.

## Repository map

- `src/Forma.Core`: immutable domain types, validation and pure tracking/metric functions.
- `src/Forma.Web`: Blazor UI, reusable Razor components, state container and JavaScript storage adapter.
- `tests`: automated tests and run instructions.
- `docs`: project workflow, implementation decisions, validation and rubric map.
- `scripts`: static publication and asset verification helpers.

## Read next

- [Project and developer guide](docs/project-guide.md)
- [Testing commands](tests/README.md) and [verified QA results](docs/QA.md)
- [IFP rubric map](docs/assessment.md)

For a reliable defense, start the local app beforehand and use the public site as the shareable demo. Suggested demonstration: create a two-page reading habit, record partial and full completion, reload, then check History and Insights.
