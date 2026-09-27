#requires -Version 5.1
#
# Docker Model Runner quickstart (PowerShell).
# Pulls a model, lists local models, runs a one-shot prompt and prints the API endpoint.
# Idempotent: safe to run multiple times.
#
$ErrorActionPreference = "Stop"

function Invoke-Docker {
    param(
        [string[]] $Arguments,
        [switch] $Probe
    )

    # Native stderr is not itself a failure (for example, pull progress). These
    # preferences are local to this function, including on Windows PowerShell 5.1.
    $ErrorActionPreference = "Continue"
    $PSNativeCommandUseErrorActionPreference = $false
    & docker @Arguments 2>&1 | ForEach-Object {
        if (-not $Probe) { Write-Host $_ }
    }
    $exitCode = $LASTEXITCODE

    if ($Probe) { return ($exitCode -eq 0) }
    if ($exitCode -ne 0) {
        throw "docker $($Arguments -join ' ') failed (exit code $exitCode)."
    }
}

# Model can be overridden: $env:MODEL = "ai/llama3.2"; ./quickstart.ps1
$Model = if ($env:MODEL) { $env:MODEL } else { "ai/gemma3" }
$HostEndpoint = "http://localhost:12434/engines/v1"

try {
    if (-not (Get-Command docker -CommandType Application -ErrorAction SilentlyContinue)) {
        throw "Docker CLI was not found. Install Docker Desktop and make sure docker is on PATH."
    }

    Write-Host "==> 1/5 Checking that Docker Model Runner is enabled"
    Invoke-Docker -Arguments @('model', 'status')

    Write-Host "==> 2/5 Pulling model '$Model' (skipped if already present)"
    if (Invoke-Docker -Arguments @('model', 'inspect', $Model) -Probe) {
        Write-Host "    '$Model' is already available locally, skipping pull."
    } else {
        Invoke-Docker -Arguments @('model', 'pull', $Model)
    }

    Write-Host "==> 3/5 Listing local models"
    Invoke-Docker -Arguments @('model', 'list')

    Write-Host "==> 4/5 Running a one-shot prompt against '$Model'"
    Invoke-Docker -Arguments @('model', 'run', $Model, 'In one sentence, what is Docker Model Runner?')

    Write-Host "==> 5/5 OpenAI-compatible API endpoint (from the host)"
    Write-Host "    $HostEndpoint"
    Write-Host "    Try it: curl $HostEndpoint/models"
    Write-Host ""
    Write-Host "Done. The model is ready to use over the OpenAI-compatible API."
} catch {
    Write-Host "error> $($_.Exception.Message)"
    Write-Host "error> Check Docker Desktop and Settings -> AI -> Enable Docker Model Runner."
    Write-Host "error> For host API access, enable host-side TCP support on port 12434."
    exit 1
}
