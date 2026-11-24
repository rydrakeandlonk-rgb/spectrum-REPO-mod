param(
    [Parameter(Mandatory = $false)]
    [string]$GamePath = $env:REPO_GAME_PATH
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root "src/SpectrumRepoMod.csproj"
$stage = Join-Path $root "dist/Spectrum_REPO_Mod"
$zip = Join-Path $root "dist/Spectrum_REPO_Mod-0.1.0.zip"

if ([string]::IsNullOrWhiteSpace($GamePath)) {
    throw "Pass -GamePath with your R.E.P.O. install folder, or set REPO_GAME_PATH."
}
$managed = Join-Path $GamePath "REPO_Data/Managed"
if (!(Test-Path (Join-Path $managed "Assembly-CSharp.dll"))) {
    throw "Could not find REPO_Data/Managed/Assembly-CSharp.dll under '$GamePath'."
}

if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
if (Test-Path $zip) { Remove-Item $zip -Force }
New-Item -ItemType Directory -Path (Join-Path $root "dist") -Force | Out-Null

dotnet build $project --configuration Release "-p:REPOGamePath=$GamePath"
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
Write-Host "Thunderstore package created: $zip"
