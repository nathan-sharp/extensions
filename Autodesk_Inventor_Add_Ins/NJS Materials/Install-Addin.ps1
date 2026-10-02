$ErrorActionPreference = 'Stop'

if (-not [Environment]::Is64BitProcess) { throw 'Run this script from 64-bit PowerShell.' }

$projectRoot = $PSScriptRoot
$buildDirectory = Join-Path $projectRoot 'bin\Release\net8.0-windows'
$templatePath = Join-Path $projectRoot 'NJS.InventorMaterials.addin.template'
if (-not (Test-Path (Join-Path $buildDirectory 'NJS.InventorMaterials.dll'))) {
    dotnet build (Join-Path $projectRoot 'NJS.InventorMaterials.csproj') -c Release
}

$assemblyPath = Join-Path $buildDirectory 'NJS.InventorMaterials.dll'
$comHostPath = Join-Path $buildDirectory 'NJS.InventorMaterials.comhost.dll'
foreach ($path in @($assemblyPath, $comHostPath, $templatePath)) {
    if (-not (Test-Path $path)) { throw "Required file not found: $path" }
}

$addinDirectory = Join-Path $env:APPDATA 'Autodesk\Inventor 2026\Addins'
$installRoot = Join-Path $env:LOCALAPPDATA 'NJSMaterials\InventorAddIn'
$buildId = (Get-FileHash $assemblyPath -Algorithm SHA256).Hash.Substring(0, 12)
$installDirectory = Join-Path $installRoot $buildId
New-Item -ItemType Directory -Path $installDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $addinDirectory -Force | Out-Null
Copy-Item (Join-Path $buildDirectory '*') $installDirectory -Force

$installedAssembly = Join-Path $installDirectory 'NJS.InventorMaterials.dll'
$installedComHost = Join-Path $installDirectory 'NJS.InventorMaterials.comhost.dll'
$addinManifest = Join-Path $addinDirectory 'NJS.InventorMaterials.addin'
$classId = '{6C5C1A92-4C4E-4B8E-9E74-7D9A7B4D2F31}'
$progId = 'NJS.InventorMaterials.StandardAddInServer'
$manifest = (Get-Content $templatePath -Raw).Replace('__ADDIN_ASSEMBLY_PATH__', $installedAssembly)

$classesKey = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey('Software\Classes')
try {
    $classKey = $classesKey.CreateSubKey("CLSID\$classId")
    $classKey.SetValue('', 'NJS Materials Inventor Add-In')
    $inprocKey = $classKey.CreateSubKey('InprocServer32')
    $inprocKey.SetValue('', $installedComHost)
    $inprocKey.SetValue('ThreadingModel', 'Both')
    $inprocKey.Dispose()
    $classKey.Dispose()
    $progIdKey = $classesKey.CreateSubKey($progId)
    $progIdKey.SetValue('', 'NJS Materials Inventor Add-In')
    $progIdClsidKey = $progIdKey.CreateSubKey('CLSID')
    $progIdClsidKey.SetValue('', $classId)
    $progIdClsidKey.Dispose()
    $progIdKey.Dispose()
    Set-Content -Path $addinManifest -Value $manifest -Encoding UTF8
}
finally {
    $classesKey.Dispose()
}

Write-Host 'NJS Materials installed for the current Windows user. Restart Inventor 2026.'