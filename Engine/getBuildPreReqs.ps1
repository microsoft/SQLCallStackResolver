# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License - see LICENSE file in this repo.

# Find out the installation path for the highest version of Visual Studio installed
$vsMaxVerFound = 0
$vsPath = $null
$vsVersions = (& "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe" -legacy -prerelease -format json) | ConvertFrom-Json
foreach ($vsInstance in $vsVersions) {
    $vsFullVersion = $vsInstance.installationVersion
    $verFound = [int]::Parse($vsFullVersion.Split(".")[0])
    if ($verFound -gt $vsMaxVerFound) {
        $vsMaxVerFound = $verFound
        $vsPath = $vsInstance.installationPath
    }    
}

if ($null -eq $vsPath) {
    Write-Error "No Visual Studio installation found. Exiting..." -ErrorAction Stop    
}

Write-Host "Using Visual Studio $vsFullVersion from $vsPath"

if (-not (Test-Path "./DIA")) {
    mkdir DIA
}

copy "$vsPath\Team Tools\Performance Tools\x64\msdia140.dll" "./DIA/msdia140.dll"
copy "$vsPath\Team Tools\Performance Tools\x64\msdia140.dll.manifest" "./DIA/msdia140.dll.manifest"
copy "$vsPath\Common7\IDE\Remote Debugger\x64\msvcp140.dll" "./DIA/msvcp140.dll"
copy "$vsPath\Common7\IDE\Remote Debugger\x64\vcruntime140.dll" "./DIA/vcruntime140.dll"
copy "$vsPath\Common7\IDE\Remote Debugger\x64\vcruntime140_1.dll" "./DIA/vcruntime140_1.dll"

Write-Host
Write-Host "DIA file versions:"
$diaFiles = dir "./DIA/*.dll"
foreach ($file in $diaFiles) {
    Write-Host $file.FullName":" $file.VersionInfo.FileVersion
}

if ((Get-ChildItem ".\DIA\*.*").Length -ne 5) {
    Write-Error "You must manually obtain msdia140.dll, msdia140.dll.manifest and associated necessary Visual C++ runtime dependency DLLs (msvcp140.dll, vcruntime140.dll and vcruntime140_1.dll). Those are redistributable components of Visual Studio 2019 subject to terms as published here: https://docs.microsoft.com/en-us/visualstudio/releases/2019/redistribution." -ErrorAction Stop
}

Write-Host "Debugger file versions:"
$dbgFiles = @(dir "../packages/Microsoft.Debugging.Platform.DbgEng.*/content/amd64/dbghelp.dll")
$dbgFiles += dir "../packages/Microsoft.Debugging.Platform.SymSrv.*/content/amd64/symsrv.dll"
foreach ($file in $dbgFiles) {
    Write-Host $file.FullName":" $file.VersionInfo.FileVersion
}
