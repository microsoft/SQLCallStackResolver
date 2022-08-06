# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License - see LICENSE file in this repo.

$vsMaxVerFound = 0
$vsPath = (& "${env:ProgramFiles(x86)}/Microsoft Visual Studio/Installer/vswhere.exe" -latest -prerelease -property installationPath)
if ($vsPath -eq $null) { Write-Error "No Visual Studio installation found. Exiting..." -ErrorAction Stop }
else { Write-Host "Using Visual Studio from $vsPath" }

& "${env:COMSPEC}" /s /c "`"$vsPath\Common7\Tools\vsdevcmd.bat`" -no_logo && set" | foreach-object {
    $name, $value = $_ -split '=', 2
    set-content env:\"$name" $value
    }
