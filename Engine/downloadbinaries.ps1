# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License - see LICENSE file in this repo.

param ($binaryDnldURL)
$localpath = ".\BinaryDependencies.zip"

if (-not (test-path $localpath)) {
    try {
        Invoke-WebRequest -UseBasicParsing -uri $binaryDnldURL -OutFile $localpath -ErrorAction Ignore
        Expand-Archive -Path $localpath -DestinationPath "."
    } catch {}
}

if (-not (test-path (".\DIA\msdia140.dll")))    {
    Write-Warning "You must manually obtain msdia140.dll, msdia140.dll.manifest and associated necessary Visual C++ runtime dependency DLLs (msvcp140.dll, vcruntime140.dll and vcruntime140_1.dll are redistributable components of Visual Studio 2019 subject to terms as published [here](https://docs.microsoft.com/en-us/visualstudio/releases/2019/redistribution). Windows Debugging Tools DLLs (dbghelp.dll and symsrv.dll) as per the terms published at https://docs.microsoft.com/en-us/legal/windows-sdk/redist#debugging-tools-for-windows."
}
