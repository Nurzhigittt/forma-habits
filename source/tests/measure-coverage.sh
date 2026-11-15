#!/usr/bin/env bash
# Requires a Debug test build and Coverlet Console 6.0.4, e.g.:
#   dotnet test Forma.sln -c Debug
#   dotnet tool install --global coverlet.console --version 6.0.4
set -euo pipefail
cd "$(dirname "$0")/.."

out="${1:-TestResults/coverage}"
mkdir -p "$out"
coverlet="${COVERLET:-coverlet}"
unit=tests/Forma.Core.Tests
integration=tests/Forma.Integration.Tests

# Keep the two coverages separate: they have explicit, different scopes.
"$coverlet" "$unit/bin/Debug/net10.0/Forma.Core.Tests.dll" --target dotnet \
  --targetargs "test $unit/Forma.Core.Tests.csproj -c Debug --no-build --no-restore" \
  --include '[Forma.Core]*' --format cobertura --output "$out/core-only.xml"
"$coverlet" "$integration/bin/Debug/net10.0/Forma.Integration.Tests.dll" --target dotnet \
  --targetargs "test $integration/Forma.Integration.Tests.csproj -c Debug --no-build --no-restore" \
  --include '[Forma.Web]Forma.Web.State.HabitStore*' \
  --include '[Forma.Web]Forma.Web.State.BrowserStorage*' \
  --format cobertura --output "$out/store-only.xml"
