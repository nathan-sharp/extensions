$ErrorActionPreference = 'Stop'

if (-not [Environment]::Is64BitProcess) { throw 'Run this script from 64-bit PowerShell.' }

$installDirectory = Join-Path $env:LOCALAPPDATA 'NJSMaterials\InventorAddIn'
$addinManifest = Join-Path $env:APPDATA 'Autodesk\Inventor 2026\Addins\NJS.InventorMaterials.addin'
$classId = '{6C5C1A92-4C4E-4B8E-9E74-7D9A7B4D2F31}'
$progId = 'NJS.InventorMaterials.StandardAddInServer'
$classesKey = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Software\Classes', $true)
if ($classesKey) {
    try {
        $classesKey.DeleteSubKeyTree("CLSID\$classId", $false)
        $classesKey.DeleteSubKeyTree($progId, $false)
    }
    finally { $classesKey.Dispose() }
}
if (Test-Path $addinManifest) { Remove-Item $addinManifest -Force }
if (Test-Path $installDirectory) { Remove-Item $installDirectory -Recurse -Force }
Write-Host 'NJS Materials removed for the current Windows user. Restart Inventor if it was open.'