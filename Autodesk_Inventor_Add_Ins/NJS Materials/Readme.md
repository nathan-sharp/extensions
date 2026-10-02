# NJS Materials for Autodesk Inventor

NJS Materials is a separate Autodesk Inventor 2026 add-in for applying standards-based physical materials to part documents.

## Features

The **Material Standards** command appears in the **Tools** tab of the **Part** environment. The initial catalog includes AISI 4140, AISI 1045, AISI 304, AISI 316, and 6061-T6.

The add-in uses physical properties from Autodesk Inventor material-library assets. It does not create guessed material data. Enable the relevant material library in the active Inventor project for a standard to show as available.

The first version applies materials to part documents. Assembly support will need to assign materials to the referenced component parts or occurrences and is intentionally separate from this initial command.

## Build

From Windows PowerShell in this folder:

```powershell
dotnet build .\NJS.InventorMaterials.csproj -c Release
```

If Inventor is installed elsewhere:

```powershell
dotnet build .\NJS.InventorMaterials.csproj -c Release -p:InventorBinPath="D:\Autodesk\Inventor 2026\Bin"
```

## Install

Close Inventor, then run `Install-Addin.ps1` from 64-bit PowerShell. The installer registers this add-in independently from NJS Tools and writes its manifest to the Inventor 2026 per-user add-in folder.

## Requirements

- Autodesk Inventor Professional 2026, 64-bit
- .NET 8 Desktop Runtime and SDK
- 64-bit Windows PowerShell