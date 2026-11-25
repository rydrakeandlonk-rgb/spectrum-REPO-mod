$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root "src/SpectrumRepoMod.csproj"
$stage = Join-Path $root "dist/Spectrum_REPO_Mod"
$manifest = Get-Content (Join-Path $root "manifest.json") -Raw | ConvertFrom-Json
$zip = Join-Path $root ("dist/Spectrum_REPO_Mod-{0}.zip" -f $manifest.version_number)

if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
if (Test-Path $zip) { Remove-Item $zip -Force }
New-Item -ItemType Directory -Path (Join-Path $root "dist") -Force | Out-Null

dotnet restore $project
if ($LASTEXITCODE -ne 0) { throw "Dependency restore failed." }

dotnet build $project --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { throw "The R.E.P.O. plugin build failed." }

$required = @(
    (Join-Path $stage "BepInEx/plugins/SpectrumRepoMod.dll"),
    (Join-Path $stage "manifest.json"),
    (Join-Path $stage "README.md"),
    (Join-Path $stage "CHANGELOG.md"),
    (Join-Path $stage "icon.png")
)
foreach ($file in $required) {
    if (!(Test-Path $file)) { throw "Package output is missing: $file" }
}

Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $zip -CompressionLevel Optimal
Write-Host "Vortex install package created: $zip"
