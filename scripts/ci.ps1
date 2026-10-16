# Local CI: the same checks GitHub Actions runs. Usage: ./scripts/ci.ps1 [-Target all|backend|frontend]
param([ValidateSet('all', 'backend', 'frontend')][string]$Target = 'all')

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
if (-not $env:CI) { $env:CI = 'true' } # enables NuGet locked-mode restore (see Directory.Build.props)

function Step([string]$Name) { Write-Host "`n==> $Name" -ForegroundColor Cyan }
function Run([scriptblock]$Command) {
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "Command failed ($LASTEXITCODE): $Command" }
}

function Backend {
    Push-Location (Join-Path $Root 'backend')
    try {
        Step 'backend: restore (locked)'
        Run { dotnet tool restore }
        Run { dotnet restore --locked-mode }
        Step 'backend: format check (csharpier)'
        Run { dotnet csharpier check . }
        Step 'backend: build (warnings as errors)'
        Run { dotnet build --no-restore -c Release }
        Step 'backend: test'
        Run { dotnet test --no-build -c Release }
    }
    finally { Pop-Location }
}

function Frontend {
    Push-Location $Root
    try {
        Step 'frontend: install (frozen lockfile)'
        Run { pnpm install --frozen-lockfile }
        Step 'frontend: audit'
        Run { pnpm audit --prod }
    }
    finally { Pop-Location }

    Push-Location (Join-Path $Root 'frontend')
    try {
        Step 'frontend: lint'
        Run { pnpm lint }
        Step 'frontend: format check'
        Run { pnpm format:check }
        Step 'frontend: test'
        Run { pnpm test }
        Step 'frontend: build'
        Run { pnpm build }
    }
    finally { Pop-Location }
}

if ($Target -in 'all', 'backend') { Backend }
if ($Target -in 'all', 'frontend') { Frontend }
Write-Host "`n==> CI passed" -ForegroundColor Green
