# FORMA | Quality assurance record

Verification date: November 15, 2026.

**Latest successful full CI run:** [Verify FORMA #2](https://github.com/Nurzhigittt/forma-habits/actions/runs/43728705886), commit `333570739aca14e571bef9b11ce91c898ffafa30`.

| Area | Verified result |
| --- | --- |
| xUnit unit | **21 passed**, zero failures/skips. Pure domain schedules, immutable updates, validation, progress and metrics. |
| xUnit integration | **11 passed**, zero failures/skips. Real HabitStore, versioned JSON and BrowserStorage with the controlled IJSRuntime test double. These passed through the normal full solution test command on GitHub Actions. |
| Release build | **Passed**: Blazor WebAssembly release publication completed successfully. |
| Browser end-to-end | **17 passed**, zero failures, on GitHub Actions against the live GitHub Pages website. |
| Browser engine matrix | Five scenarios each in desktop Chromium, Firefox and WebKit; one mobile workflow each in Android and iOS emulation. |
| Workflow scenarios | Numeric progress and reload; History/Insights; JSON export/restore and invalid backup rejection; edit/delete and cancellation; storage write failure; narrow layout, editor focus, creating/completing a habit and opening History. |
| Coverage | Scoped Coverlet runs completed. Forma.Core line coverage **93.84%**; HabitStore + BrowserStorage scoped line coverage **100%**. These are not whole-application coverage figures. See CI artifacts for current XML. |
| Native Safari / physical devices | **Not verified.** WebKit is an engine test; mobile profiles are emulation, not real iPhones or Android phones. |

## Reproduce and inspect evidence

- [Testing guide](../tests/README.md)
- [Current CI workflow](../../.github/workflows/verify.yml)
- [Run logs and downloadable artifacts](https://github.com/Nurzhigittt/forma-habits/actions/runs/43728705886): `domain-and-coverage` and `browser-report`.
- Prior scoped Cobertura evidence: [Core](evidence/core-only.xml) and [persistence boundary](evidence/store-only.xml).
- Earlier in-process xUnit rerun XML: [unit](evidence/verified-unit.xml) and [integration](evidence/verified-integration.xml). The integration XML used the documented isolated harness because the local runtime could not launch the WebAssembly task host. The full GitHub CI result above supersedes that environment limitation.

The browser suite uses isolated contexts and synthetic habits, and does not edit a user's existing records. Production application code and the published deployment archive were unchanged by this source/test documentation publication.

## Scope and limitations

Data remains local to the site origin and browser profile. There is no account or automatic synchronization. Simultaneous edits in different tabs are not synchronized; use one tab when editing.
No physical-device test, native macOS Safari test, large-scale load test or overall application coverage percentage is claimed.

## Manual observations before CI

Desktop Chromium had also been checked for logging, reload, review screens, edit/delete, invalid backup rejection and restore. Narrow viewport checks used 390×844 and 393×851 CSS pixels. These historical observations are separate from the automated engine matrix above.
