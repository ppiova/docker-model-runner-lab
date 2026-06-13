#requires -Version 5.1
#
# Docker Model Runner quickstart (PowerShell).
# Pulls a model, lists local models, runs a one-shot prompt and prints the API endpoint.
# Idempotent: safe to run multiple times.
#
$ErrorActionPreference = "Stop"

# Model can be overridden: $env:MODEL = "ai/llama3.2"; ./quickstart.ps1
$Model = if ($env:MODEL) { $env:MODEL } else { "ai/gemma3" }
$HostEndpoint = "http://localhost:12434/engines/v1"

Write-Host "==> 1/5 Checking that Docker Model Runner is enabled"
docker model status *> $null
if ($LASTEXITCODE -ne 0) {
    Write-Host "    Docker Model Runner is not running."
    Write-Host "    Enable it in Docker Desktop: Settings -> AI -> Enable Docker Model Runner"
    Write-Host "    (also enable host-side TCP support on port 12434)."
    exit 1
}
docker model status

Write-Host "==> 2/5 Pulling model '$Model' (skipped if already present)"
$existing = docker model list | Select-String -SimpleMatch $Model
if ($existing) {
    Write-Host "    '$Model' is already available locally, skipping pull."
} else {
    docker model pull $Model
}

Write-Host "==> 3/5 Listing local models"
docker model list

Write-Host "==> 4/5 Running a one-shot prompt against '$Model'"
docker model run $Model "In one sentence, what is Docker Model Runner?"

Write-Host "==> 5/5 OpenAI-compatible API endpoint (from the host)"
Write-Host "    $HostEndpoint"
Write-Host "    Try it: curl $HostEndpoint/models"
Write-Host ""
Write-Host "Done. The model is ready to use over the OpenAI-compatible API."
