#requires -Version 5.1
$ErrorActionPreference = 'Stop'
$originalModel = $env:MODEL
try {
    foreach ($module in @('04-compose', '05-blazor-chat')) {
        $composePath = Join-Path $PSScriptRoot "../$module/compose.yaml"
        foreach ($case in @(
            @{ Name = 'unset'; Value = $null; Expected = 'ai/gemma3' },
            @{ Name = 'empty'; Value = ''; Expected = 'ai/gemma3' },
            @{ Name = 'override'; Value = 'ai/test-model'; Expected = 'ai/test-model' }
        )) {
            $env:MODEL = $case.Value
            # Some Compose versions omit top-level models from JSON output.
            $yaml = (& docker compose -f $composePath config) -join "`n"
            if ($LASTEXITCODE -ne 0) { throw "Compose validation failed for $module." }
            $expectedModel = [regex]::Escape($case.Expected)
            if ($yaml -notmatch "(?m)^models:\r?\n  gemma:\r?\n    model: $expectedModel\r?$") {
                throw "$module ($($case.Name)): expected model $($case.Expected).`n$yaml"
            }
            $json = & docker compose -f $composePath config --format json
            if ($LASTEXITCODE -ne 0) { throw "Compose JSON validation failed for $module." }
            $config = ($json -join "`n") | ConvertFrom-Json
            $service = @($config.services.PSObject.Properties)[0].Value
            if ($service.models.gemma.model_var -ne 'MODEL' -or
                $service.models.gemma.endpoint_var -ne 'OPENAI_BASE_URL') {
                throw "${module}: model binding is missing."
            }
            Write-Host "PASS: $module ($($case.Name))"
        }
    }
} finally {
    $env:MODEL = $originalModel
}
