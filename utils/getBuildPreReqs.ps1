# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License - see LICENSE file in this repo.

# This script should be executed with the Engine folder as the working folder

. "$PSScriptRoot/import-vsenv.ps1"  # import VS environment

if (-not (Test-Path DIA/Dia2Lib.dll)) {
    pushd "$env:TEMP"
    midl.exe /x64 /I "$env:VSINSTALLDIR/DIA SDK/include" "$env:VSINSTALLDIR/DIA SDK/idl/dia2.idl" /tlb dia2.tlb
    popd
    tlbimp.exe /machine:x64 "$env:TEMP/dia2.tlb" /out:"DIA/Dia2Lib.dll"
}

copy "$env:VSINSTALLDIR/DIA SDK/bin/amd64/msdia140.dll" "DIA/msdia140.dll"
@(dir "DIA/msdia140.dll").VersionInfo.ToString()
& mt.exe -tlb:"DIA/msdia140.dll" -dll:"DIA/msdia140.dll" -out:"DIA/msdia140.dll.manifest"

if ((dir "DIA/*").Length -ne 3) 
{ Write-Error "DIA SDK not found / typelib / manifest missing. Exiting..." -ErrorAction Stop }

# fix the file path to msdia140.dll within the manifest
$diaManifestPath = "DIA/msdia140.dll.manifest"
(Get-Content $diaManifestPath).Replace("DIA/msdia140.dll", "msdia140.dll") | Set-Content $diaManifestPath

@(dir "../packages/Microsoft.Debugging.Platform.DbgEng.20220711.1523.0/content/amd64/dbghelp.dll").VersionInfo.ToString()
@(dir "../packages/Microsoft.Debugging.Platform.SymSrv.20220711.1523.0/content/amd64/symsrv.dll").VersionInfo.ToString()
