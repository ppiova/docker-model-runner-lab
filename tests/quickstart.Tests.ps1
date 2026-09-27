#requires -Version 5.1
# Exercises the real script in a child shell with a fake native Docker CLI.
$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot '../01-quickstart/quickstart.ps1'
$windowsHost = $env:OS -eq 'Windows_NT'
$shellName = if ($PSVersionTable.PSEdition -eq 'Desktop') { 'powershell.exe' } elseif ($windowsHost) { 'pwsh.exe' } else { 'pwsh' }
$shellPath = Join-Path $PSHOME $shellName
$shellArguments = @('-NoProfile')
if ($windowsHost) { $shellArguments += @('-ExecutionPolicy', 'Bypass') }
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('dmr-test-' + [guid]::NewGuid())
$originalEnvironment = @{}
$variables = @('PATH', 'MODEL', 'DMR_TEST_LOG', 'DMR_TEST_FAIL', 'DMR_TEST_EXISTS')
foreach ($name in $variables) { $originalEnvironment[$name] = [Environment]::GetEnvironmentVariable($name) }

try {
    New-Item -ItemType Directory -Path $testRoot | Out-Null
    $emptyDirectory = Join-Path $testRoot 'empty'
    New-Item -ItemType Directory -Path $emptyDirectory | Out-Null
    $env:DMR_TEST_LOG = Join-Path $testRoot 'commands.log'
    $env:MODEL = 'ai/test-model'
    $wrapper = Join-Path $testRoot 'invoke.ps1'
    @'
param([string] $ScriptPath, [int] $NativeErrors)
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = [bool] $NativeErrors
& $ScriptPath
exit $LASTEXITCODE
'@ | Set-Content -LiteralPath $wrapper

    if ($windowsHost) {
        @'
@echo off
echo %2>>"%DMR_TEST_LOG%"
if "%2"=="%DMR_TEST_FAIL%" (
  echo simulated %2 failure 1>&2
  exit /b 17
)
if "%2"=="inspect" if "%DMR_TEST_EXISTS%"=="0" (
  echo model is absent 1>&2
  exit /b 1
)
echo simulated %2 progress 1>&2
exit /b 0
'@ | Set-Content -LiteralPath (Join-Path $testRoot 'docker.cmd') -Encoding Ascii
    } else {
        $fakeDocker = Join-Path $testRoot 'docker'
        $fakeScript = @'
#!/bin/sh
printf '%s\n' "$2" >> "$DMR_TEST_LOG"
if [ "$2" = "$DMR_TEST_FAIL" ]; then
  echo "simulated $2 failure" >&2
  exit 17
fi
if [ "$2" = inspect ] && [ "$DMR_TEST_EXISTS" = 0 ]; then
  echo 'model is absent' >&2
  exit 1
fi
echo "simulated $2 progress" >&2
exit 0
'@
        # .gitattributes checks out .ps1 files with CRLF even on Linux. A native
        # shell script needs LF, including its shebang, and no UTF-8 BOM.
        [IO.File]::WriteAllText($fakeDocker, $fakeScript.Replace("`r`n", "`n") + "`n")
        & chmod +x $fakeDocker
        if ($LASTEXITCODE -ne 0) { throw 'Could not make the fake Docker executable.' }
    }

    $cases = @(
        @{ Name = 'existing model'; Exists = '1'; Fail = ''; Commands = 'status,inspect,list,run'; Success = $true },
        @{ Name = 'missing model'; Exists = '0'; Fail = ''; Commands = 'status,inspect,pull,list,run'; Success = $true },
        @{ Name = 'status failure'; Exists = '0'; Fail = 'status'; Commands = 'status'; Success = $false },
        @{ Name = 'pull failure'; Exists = '0'; Fail = 'pull'; Commands = 'status,inspect,pull'; Success = $false },
        @{ Name = 'list failure'; Exists = '1'; Fail = 'list'; Commands = 'status,inspect,list'; Success = $false },
        @{ Name = 'run failure'; Exists = '1'; Fail = 'run'; Commands = 'status,inspect,list,run'; Success = $false },
        @{ Name = 'missing CLI'; Exists = '0'; Fail = ''; Commands = ''; Success = $false }
    )

    foreach ($nativeErrors in @(0, 1)) {
        foreach ($case in $cases) {
            $env:PATH = if ($case.Name -eq 'missing CLI') { $emptyDirectory } else { $testRoot }
            $env:DMR_TEST_FAIL = $case.Fail
            $env:DMR_TEST_EXISTS = $case.Exists
            [IO.File]::WriteAllText($env:DMR_TEST_LOG, '')
            # The child shell must be allowed to fail so we can assert its exit code.
            $ErrorActionPreference = 'Continue'
            $PSNativeCommandUseErrorActionPreference = $false
            $output = (& $shellPath @shellArguments -File $wrapper $scriptPath $nativeErrors 2>&1 | Out-String)
            $exitCode = $LASTEXITCODE
            $ErrorActionPreference = 'Stop'
            $commands = @(Get-Content -LiteralPath $env:DMR_TEST_LOG) -join ','
            if (($exitCode -eq 0) -ne $case.Success -or
                $output.Contains('Done.') -ne $case.Success -or
                $commands -ne $case.Commands) {
                throw "$($case.Name) (native errors=$nativeErrors): exit=$exitCode commands=$commands`n$output"
            }
            if (-not $case.Success -and -not $output.Contains('error>')) {
                throw "$($case.Name): missing actionable error.`n$output"
            }
            Write-Host "PASS: $($case.Name) (native errors=$nativeErrors)"
        }
    }
} finally {
    foreach ($name in $variables) { [Environment]::SetEnvironmentVariable($name, $originalEnvironment[$name]) }
    # Delete only the unique directory created by this test under the temp root.
    $resolvedRoot = [IO.Path]::GetFullPath($testRoot)
    $tempPrefix = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if ($resolvedRoot.StartsWith($tempPrefix, [StringComparison]::OrdinalIgnoreCase) -and
        (Split-Path $resolvedRoot -Leaf) -like 'dmr-test-*') {
        Remove-Item -LiteralPath $resolvedRoot -Recurse -Force
    }
}

# GitHub Actions propagates LASTEXITCODE after invoking a script. The last
# scenario intentionally fails, but reaching here means every assertion passed.
exit 0
