<#
  Builds an unsigned MSIX for Microsoft Store upload — no Visual Studio needed.

    .\StoreAssets\build-msix.ps1                      # x64
    .\StoreAssets\build-msix.ps1 -Arch x64,arm64      # one .msix per arch + .msixbundle

  Output: StoreAssets\out\HueControl_<ver>_<arch>.msix (+ .msixbundle when several archs).
  The Store re-signs the package, so no certificate is needed for upload.

  Identity (Name/Publisher) comes from StoreAssets\Package.appxmanifest and must
  match Partner Center → Product identity exactly. Version comes from
  <Version> in HueControl.csproj (x.y.z → x.y.z.0).
#>
param(
    [string[]]$Arch = @('x64'),
    [switch]$Register   # x64 only: register the loose layout locally for a smoke test (needs Developer Mode)
)

$ErrorActionPreference = 'Stop'
$root   = Split-Path $PSScriptRoot -Parent
$assets = $PSScriptRoot
$out    = Join-Path $assets 'out'
$proj   = Join-Path $root 'HueControl\HueControl.csproj'

# Newest Windows SDK that has the packaging tools.
$sdkBin = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' -Directory |
    Where-Object { Test-Path (Join-Path $_.FullName 'x64\makeappx.exe') } |
    Sort-Object { [version]$_.Name } | Select-Object -Last 1
if (-not $sdkBin) { throw 'Windows 10/11 SDK with makeappx.exe not found.' }
$makeappx = Join-Path $sdkBin.FullName 'x64\makeappx.exe'
$makepri  = Join-Path $sdkBin.FullName 'x64\makepri.exe'

[xml]$csproj = Get-Content $proj
$ver = ($csproj.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { $_ } | Select-Object -First 1)
$pkgVer = "$ver.0"

if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory $out | Out-Null

$built = @()
foreach ($a in $Arch) {
    $layout = Join-Path $out "layout-$a"
    Write-Host "== $a : publish" -ForegroundColor Cyan
    # Self-contained: the Store does not install the .NET Desktop Runtime for full-trust apps.
    dotnet publish $proj -c Release -r "win-$a" --self-contained true `
        -p:PublishReadyToRun=true -o (Join-Path $layout 'HueControl')
    if ($LASTEXITCODE) { throw "dotnet publish failed ($a)" }

    Copy-Item (Join-Path $assets 'Images') (Join-Path $layout 'Images') -Recurse

    [xml]$m = Get-Content (Join-Path $assets 'Package.appxmanifest')
    $m.Package.Identity.SetAttribute('Version', $pkgVer)
    $m.Package.Identity.SetAttribute('ProcessorArchitecture', $a)
    $m.Save((Join-Path $layout 'AppxManifest.xml'))

    # resources.pri lets Windows pick the scale/targetsize logo variants.
    Push-Location $layout
    & $makepri createconfig /cf priconfig.xml /dq en-US /o | Out-Null
    & $makepri new /pr . /cf priconfig.xml /of resources.pri /o | Out-Null
    if ($LASTEXITCODE) { Pop-Location; throw "makepri failed ($a)" }
    Remove-Item priconfig.xml
    Pop-Location

    $msix = Join-Path $out "HueControl_${pkgVer}_$a.msix"
    Write-Host "== $a : pack" -ForegroundColor Cyan
    & $makeappx pack /d $layout /p $msix /o
    if ($LASTEXITCODE) { throw "makeappx pack failed ($a)" }
    $built += $msix
}

if ($built.Count -gt 1) {
    $bundleDir = Join-Path $out 'bundle'
    New-Item -ItemType Directory $bundleDir | Out-Null
    $built | Copy-Item -Destination $bundleDir
    & $makeappx bundle /d $bundleDir /p (Join-Path $out "HueControl_${pkgVer}.msixbundle") /bv $pkgVer /o
    if ($LASTEXITCODE) { throw 'makeappx bundle failed' }
    Remove-Item $bundleDir -Recurse
}

if ($Register) {
    Add-AppxPackage -Register (Join-Path $out 'layout-x64\AppxManifest.xml')
    $name = ([xml](Get-Content (Join-Path $assets 'Package.appxmanifest'))).Package.Identity.Name
    Write-Host "Registered. Remove with: Get-AppxPackage $name | Remove-AppxPackage"
}

Write-Host "`nDone:" -ForegroundColor Green
Get-ChildItem $out -File | ForEach-Object { '  {0}  ({1:N1} MB)' -f $_.Name, ($_.Length / 1MB) }
