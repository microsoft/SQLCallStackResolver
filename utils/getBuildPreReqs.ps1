# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License - see LICENSE file in this repo.

$NUGET_BASE = "$HOME/.nuget"

. "$PSScriptRoot/import-vsenv.ps1"  # import VS environment

if (-not (Test-Path DIA/Dia2Lib.dll)) {
    pushd "$env:TEMP"
    midl.exe /x64 /I "$env:VSINSTALLDIR/DIA SDK/include" "$env:VSINSTALLDIR/DIA SDK/idl/dia2.idl" /tlb dia2.tlb
    popd
    tlbimp.exe "$env:TEMP/dia2.tlb" /machine:x64 /out:"DIA/Dia2Lib.dll"
}

copy "$env:VSINSTALLDIR/DIA SDK/bin/amd64/msdia140.dll" "DIA/msdia140.dll"
@(dir "DIA/msdia140.dll").VersionInfo.ToString()
& mt.exe -tlb:"DIA/msdia140.dll" -dll:"DIA/msdia140.dll" -out:"DIA/msdia140.dll.manifest"

if ((dir "DIA/*").Length -ne 3) 
{ Write-Error "DIA SDK not found / typelib / manifest missing. Exiting..." -ErrorAction Stop }

# fix the file path to msdia140.dll within the manifest
$diaManifestPath = "DIA/msdia140.dll.manifest"
(Get-Content $diaManifestPath).Replace("DIA/msdia140.dll", "msdia140.dll") -Replace " description", " threadingModel=`"Both`" description " | Set-Content $diaManifestPath

@(dir "$NUGET_BASE/packages/Microsoft.Debugging.Platform.DbgEng/20220801.1622.0/content/amd64/dbghelp.dll").VersionInfo.ToString()
@(dir "$NUGET_BASE/packages/Microsoft.Debugging.Platform.SymSrv/20220801.1622.0/content/amd64/symsrv.dll").VersionInfo.ToString()
