# Regression checks

Run from the repository root with PowerShell 7:

```powershell
pwsh -NoProfile -File tests/quickstart.Tests.ps1
pwsh -NoProfile -File tests/compose.Tests.ps1
```

On Windows, also run `powershell -NoProfile -ExecutionPolicy Bypass -File tests/quickstart.Tests.ps1`
to check Windows PowerShell 5.1 compatibility. The execution policy option applies only to
the test process; it does not change the machine's configured policy.

The quickstart tests use a temporary fake native Docker command and do not need Docker
or a model. They check existing/missing models, CLI availability, command failures,
stderr progress and both native error preference settings. Environment changes and
temporary files are cleaned up after the run.

The Compose checks require the Docker Compose plugin with `models` support. They only
resolve configuration for unset, empty and overridden `MODEL`; no containers are started
and no models are downloaded. PowerShell versions that treat an empty environment variable
as absent exercise the default path twice; CI uses PowerShell 7 on Linux to cover empty values.
