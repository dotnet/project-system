# Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

# Gets the path to MSBuild via vswhere.exe. Returns nothing if a suitable Visual Studio version is not installed.

param ([Parameter(Mandatory=$true)] [String] $versionJsonPath)

# https://github.com/microsoft/vswhere/wiki/Installing
$installerPath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer"
if(-Not (Test-Path -Path $installerPath))
{
  return
}

$versionJson = Get-Content $versionJsonPath | ConvertFrom-Json
# Use only the Major version value from the version number.
$minimumVersion = "$(($versionJson.version.Split('.'))[0]).0"

# Note: Along with VS installations, this finds BuildTools' MSBuild.exe via '-products *'. However, the repo currently cannot deploy the VS Extensions via BuildTools.
(& "$installerPath\vswhere.exe" -all -prerelease -latest -version $minimumVersion -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe) | Select-Object -First 1