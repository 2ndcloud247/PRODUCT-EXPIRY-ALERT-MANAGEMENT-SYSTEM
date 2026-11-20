# ============================================================================
#  BUILD THE INSTALLER
#
#  Double-click build-installer.bat (or run this script) to produce the files
#  you give to a client, in the "Release" folder:
#
#     ProductExpiryAlertSystem-Setup-<ver>.exe   <- give this to clients (installs .NET too if needed)
#     ProductExpiryAlertSystem-<ver>.msi         <- plain installer (PC already has .NET 4.7.2+)
#     ProductExpiryAlertSystem-<ver>.zip         <- everything above in one zip file
#
#  Needs only the .NET SDK (comes with Visual Studio). The WiX installer toolkit
#  is downloaded automatically from NuGet the first time.
# ============================================================================
param([string]$Version = "1.0.0")

$ErrorActionPreference = "Stop"
$root    = $PSScriptRoot
$appProj = Join-Path $root "PRODUCT EXPIRY ALERT MANAGEMENT SYSTEM\PRODUCT EXPIRY ALERT MANAGEMENT SYSTEM.csproj"
$appOut  = Join-Path $root "PRODUCT EXPIRY ALERT MANAGEMENT SYSTEM\bin\Release\net472"
$msiProj = Join-Path $root "Installer\Msi\ProductExpiryAlert.Msi.wixproj"
$exeProj = Join-Path $root "Installer\Bundle\ProductExpiryAlert.Bundle.wixproj"
$dist    = Join-Path $root "Release"

function Step($text) { Write-Host ""; Write-Host "==> $text" -ForegroundColor Cyan }
function Run([string[]]$cmd) {
    & dotnet @cmd
    if ($LASTEXITCODE -ne 0) { throw "Command failed: dotnet $($cmd -join ' ')" }
}

Step "1/4  Building the application (Release)"
if (Test-Path $appOut) { Remove-Item $appOut -Recurse -Force }
Run @("build", $appProj, "-c", "Release", "-p:Version=$Version", "-nologo", "-v", "q")

Step "2/4  Building the Windows Installer package (.msi)"
Run @("build", $msiProj, "-c", "Release", "-p:ProductVersion=$Version", "-p:AppDir=$appOut", "-nologo", "-v", "q")
$msi = Get-ChildItem (Join-Path $root "Installer\Msi\bin") -Recurse -Filter "ProductExpiryAlertSystem-$Version.msi" | Select-Object -First 1
if (-not $msi) { throw "MSI was not produced." }

Step "3/4  Building Setup.exe (installs .NET Framework if missing, then the .msi)"
Run @("build", $exeProj, "-c", "Release", "-p:ProductVersion=$Version", "-p:MsiFile=$($msi.FullName)", "-nologo", "-v", "q")
$setup = Get-ChildItem (Join-Path $root "Installer\Bundle\bin") -Recurse -Filter "ProductExpiryAlertSystem-Setup-$Version.exe" | Select-Object -First 1
if (-not $setup) { throw "Setup.exe was not produced." }

Step "4/4  Collecting files into the Release folder"
if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
New-Item -ItemType Directory $dist | Out-Null
Copy-Item $setup.FullName $dist
Copy-Item $msi.FullName $dist
Copy-Item (Join-Path $root "Installer\Assets\User Guide.txt") $dist
Copy-Item (Join-Path $root "Installer\Assets\INSTALLATION GUIDE.txt") $dist
$zip = Join-Path $root "ProductExpiryAlertSystem-$Version.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
# Antivirus often scans a newly copied .exe for a few seconds, so retry if the file is busy.
for ($try = 1; $try -le 10; $try++) {
    try { Compress-Archive -Path (Join-Path $dist "*") -DestinationPath $zip -Force -ErrorAction Stop; break }
    catch { if ($try -eq 10) { throw }; Start-Sleep -Seconds 3 }
}
Move-Item $zip $dist

Write-Host ""
Write-Host "Done. Files ready to give to clients:" -ForegroundColor Green
Get-ChildItem $dist | ForEach-Object { "   {0,-50} {1,8:N0} KB" -f $_.Name, ($_.Length / 1KB) }
Write-Host ""
Write-Host "Folder: $dist"
