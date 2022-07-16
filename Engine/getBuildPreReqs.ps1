# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License - see LICENSE file in this repo.

$NUGET_BASE = "$HOME/.nuget"

Write-Host "DIA file versions:"
$diaFiles = @(dir "$NUGET_BASE/packages/Microsoft.TestPlatform/*/tools/net451/Common7/IDE/Extensions/TestPlatform/x64/msdia140.dll")
foreach ($file in $diaFiles) {
    Write-Host $file.FullName":" $file.VersionInfo.FileVersion
}

if ((dir "$NUGET_BASE/packages/Microsoft.TestPlatform/*/tools/net451/Common7/IDE/Extensions/TestPlatform/x64/msdia140.*").Length -ne 2) {
    Write-Error "You must manually obtain msdia140.dll and msdia140.dll.manifest, and copy them to the Engine\DIA sub-folder. Those are redistributable components of Visual Studio 2022 subject to terms as published here: https://docs.microsoft.com/en-us/visualstudio/releases/2022/redistribution." -ErrorAction Stop
}

$diaManifestPath = "$NUGET_BASE/packages/Microsoft.TestPlatform/*/tools/net451/Common7/IDE/Extensions/TestPlatform/x64/msdia140.dll.manifest"
# fix the file path to msdia140.dll within the manifest
(Get-Content $diaManifestPath).Replace("ComComponents\x64\msdia140.dll", "msdia140.dll") | Set-Content $diaManifestPath

Write-Host "Debugger file versions:"
$dbgFiles = @(dir "$NUGET_BASE/packages/Microsoft.Debugging.Platform.DbgEng/*/content/amd64/dbghelp.dll")
$dbgFiles += dir "$NUGET_BASE/packages/Microsoft.Debugging.Platform.SymSrv/*/content/amd64/symsrv.dll"
foreach ($file in $dbgFiles) {
    Write-Host $file.FullName":" $file.VersionInfo.FileVersion
}
