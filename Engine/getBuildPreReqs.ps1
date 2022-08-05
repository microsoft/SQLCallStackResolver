# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License - see LICENSE file in this repo.

# Find out the installation path for the highest version of Visual Studio installed
$vsMaxVerFound = 0
$vsPath = $null
$vsVersions = (& "${env:ProgramFiles(x86)}/Microsoft Visual Studio/Installer/vswhere.exe" -legacy -prerelease -format json) | ConvertFrom-Json
foreach ($vsInstance in $vsVersions) {
    $vsFullVersion = $vsInstance.installationVersion
    $verFound = [int]::Parse($vsFullVersion.Split(".")[0])
    if ($verFound -gt $vsMaxVerFound) {
        $vsMaxVerFound = $verFound
        $vsPath = $vsInstance.installationPath
    }
}

if ($null -eq $vsPath) { Write-Error "No Visual Studio installation found. Exiting..." -ErrorAction Stop } 
else { Write-Host "Using Visual Studio $vsFullVersion from $vsPath" }

if (-not (Test-Path DIA/Dia2Lib.dll)) {
    $vsEnvJson = (cmd /C """$vsPath/Common7/Tools/VsDevCmd.bat"" -no_logo -arch=x64 & powershell -Command ""Get-ChildItem env: | Select-Object Key,Value | ConvertTo-Json""")
    ($vsEnvJson | ConvertFrom-Json) | ForEach-Object { $k, $v = $_.Key, $_.Value
      Set-Content env:\"$k" "$v" }
    pushd "$env:TEMP"
    midl.exe /I "$env:VSINSTALLDIR/DIA SDK/include" "$env:VSINSTALLDIR/DIA SDK/idl/dia2.idl" /tlb dia2.tlb
    popd
    tlbimp.exe "$env:TEMP/dia2.tlb" /machine:x64 /out:"DIA/Dia2Lib.dll"
}

Write-Host "DIA file versions:"
$diaFiles = @(dir "../packages/Microsoft.TestPlatform.17.2.0/tools/net451/Common7/IDE/Extensions/TestPlatform/x64/msdia140.dll")
foreach ($file in $diaFiles) { Write-Host $file.FullName":" $file.VersionInfo.FileVersion }

if ((dir "../packages/Microsoft.TestPlatform.17.2.0/tools/net451/Common7/IDE/Extensions/TestPlatform/x64/msdia140.*").Length -ne 2) {
    Write-Error "You must manually obtain msdia140.dll and msdia140.dll.manifest, and copy them to the Engine\DIA sub-folder. Those are redistributable components of Visual Studio 2022 subject to terms as published here: https://docs.microsoft.com/en-us/visualstudio/releases/2022/redistribution." -ErrorAction Stop
}

$diaManifestPath = "../packages/Microsoft.TestPlatform.17.2.0/tools/net451/Common7/IDE/Extensions/TestPlatform/x64/msdia140.dll.manifest"
# fix the file path to msdia140.dll within the manifest
(Get-Content $diaManifestPath).Replace("ComComponents\x64\msdia140.dll", "msdia140.dll") | Set-Content $diaManifestPath

Write-Host "Debugger file versions:"
$dbgFiles = @(dir "../packages/Microsoft.Debugging.Platform.DbgEng.20220711.1523.0/content/amd64/dbghelp.dll")
$dbgFiles += dir "../packages/Microsoft.Debugging.Platform.SymSrv.20220711.1523.0/content/amd64/symsrv.dll"
foreach ($file in $dbgFiles) { Write-Host $file.FullName":" $file.VersionInfo.FileVersion }
