# Forma | Testing guide

The test suite separates pure domain behavior, component integration, and browser user journeys. Each kind gives different evidence.

## 1. Unit and integration tests

Run from the project directory with the .NET 10 SDK:

```sh
dotnet test Forma.sln
dotnet test Forma.sln -c Debug
dotnet tool install --global coverlet.console --version 6.0.4
bash tests/measure-coverage.sh
```

- **Unit:** calendar boundaries, immutable updates, valid schedules, binary and numeric progress, streak calculations, completion metrics and domain validation.
- **Integration:** use the real `HabitStore`, `BrowserStorage` adapter and JSON serializer together. Replace only the external `IJSRuntime` with a controllable stub to simulate persistent data, read/write errors and recovery. This checks the .NET-to-storage contract, not actual browser storage.
- The coverage command writes Cobertura XML under `TestResults/coverage`. See the [QA record](../docs/QA.md) for actual results. Read the reported module scope; managed coverage does not measure JavaScript, browser behavior, device coverage, or every Razor interaction.

## 2. Browser end-to-end tests

Install Node.js 20 or newer. To run against the public build, start in `tests/e2e`:

```sh
npm ci
npx playwright install chromium firefox webkit
FORMA_E2E_URL=https://nurzhigittt.github.io/forma-habits/ npm test
```

On Linux CI, `npx playwright install --with-deps chromium firefox webkit` also installs engine dependencies. On PowerShell, set the URL first with `$env:FORMA_E2E_URL = 'https://nurzhigittt.github.io/forma-habits/'`, then run `npm test`.

For local testing, run `dotnet run --project src/Forma.Web/Forma.Web.csproj -- --urls http://localhost:5080` from the project root in a separate terminal, and leave `FORMA_E2E_URL` unset. You can target one project with `npm test -- --project=chromium`. The suite creates synthetic records in isolated contexts only, and Playwright retains local traces when tests fail.

The E2E scenarios cover creating and completing a habit, persistence after refresh, reviewing History and Insights, editing/deletion and backup recovery. Playwright's WebKit build is not native Safari; a device profile is emulation, not a physical phone.

## 3. Manual cross-device checklist

On each test target, record browser version, OS/device, date and result. Use a temporary synthetic habit and remove it afterward.

| Check | Expected behavior |
| --- | --- |
| Today, History, Insights | Screens open, controls stay readable, and no horizontal overflow occurs. |
| New habit | Open the form; on a phone, focus the name and target; submit with the keyboard visible or after closing it. |
| Partial then complete | A target of 2 records 1 of 2, then 2 of 2. The total is still present after reload. |
| Keyboard and cancellation | Focus is visible; Escape or Cancel exits without changing an existing habit. |
| Backup | Export is valid; invalid JSON is rejected without replacing current habits. |

For a compact defense check, run the first three steps in native macOS Safari and on an actual phone. Do not mark those rows as passed until the steps were observed on those targets.
