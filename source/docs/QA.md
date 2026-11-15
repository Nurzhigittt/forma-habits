# Forma | Quality assurance record

Verification date: November 15, 2026. Public build: [GitHub Pages](https://nurzhigittt.github.io/forma-habits/). Tests and documentation were expanded without changing the production application source.

| Area | Observed result |
| --- | --- |
| xUnit unit | **21 passed**, no failures. Tests exercise pure domain rules. |
| xUnit integration | **11 passed**, no failures. Real `HabitStore`, versioned JSON and `BrowserStorage` are tested together; only `IJSRuntime` is replaced by a controlled test double. |
| Managed code coverage | **Forma.Core: 93.84% lines (183/195), 80.76% branches (168/208).** **HabitStore + BrowserStorage: 100% lines (97/97), 70.96% branches (44/62).** These are two scoped Coverlet runs, not overall app coverage. |
| Manual desktop Chromium | Passed: quantity up to goal, reload, History/Insights, JSON backup export, malformed backup rejection, valid backup restore, edit, cancel deletion, then confirm deletion. Tested with synthetic records in an isolated preview. The public site was also verified to open without login and persist a synthetic checked habit after refresh. |
| Manual narrow Chromium viewport | Checked at **390×844** and **393×851** CSS px: no horizontal overflow, editor receives focus, create and mark a temporary habit, then open History. This is a viewport check, not a physical-device test. |
| Playwright E2E | **17 cases discovered**, not verified as passing. Five scenarios configured on each of Chromium, Firefox and WebKit, plus one on each mobile profile. The available automated runner could not start a browser. |
| Native Safari, physical iOS/Android, manual Firefox | **Not verified.** Run the checklist in the [Testing guide](../tests/README.md) before claiming device or native-browser coverage. |

Raw scoped Cobertura reports, with only source paths made relative: [Core](evidence/core-only.xml), [persistence boundary](evidence/store-only.xml).

Known limitations: browser-local persistence does not sync across devices. Simultaneous edits in multiple tabs are not synchronized; use a single tab when editing.


## Fresh rerun — 2026-11-15
**21 unit cases passed; 11 integration cases passed; zero failures or skips.** Raw xUnit XML is included as [unit evidence](evidence/verified-unit.xml) and [integration evidence](evidence/verified-integration.xml).
SDK 10.0.404, .NET runtime 10.0.15. Unit tests were built normally and run with the xUnit v3 in-process runner. The integration tests ran unchanged in a temporary test project linking the actual HabitStore and BrowserStorage source plus Forma.Core; only the existing IJSRuntime test double was used. This isolates persistence integration from the WebAssembly asset build task, which cannot launch its task host in this environment. The harness project is under verification/isolated-integration; it is an alternative verification route, not a replacement production application.
The normal full-solution build and normal CI workflow still require a standard environment. No full browser build or overall coverage rerun is claimed here.
A new Playwright Chromium/Android-emulated attempt stopped before application assertions because browser startup required a Unix socket disallowed by this runtime. This was an environment error, not a verified application test failure. Native Safari/Firefox/physical phones remain unverified.
