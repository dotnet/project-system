# Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

# Gets the path to MSBuild via vswhere.exe. Returns nothing if a suitable Visual Studio version is not installed.

param ([Parameter(Mandatory=$true)] [String] $versionJsonPath)

$installerPath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer"
if(-Not (Test-Path -Path $installerPath))
{
  return
}

$versionJson = Get-Content $versionJsonPath | ConvertFrom-Json
# Use only the Major version value from the version number.
$minimumVersion = "$(($versionJson.version.Split('.'))[0]).0"
$msBuildPath = (& "$installerPath\vswhere.exe" -all -prerelease -latest -version $minimumVersion -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe) | Select-Object -First 1

# TODO: This currently does not work for 'Build Tools for Visual Studio' because vswhere.exe does not find MSBuild located within it.
if(-Not $msBuildPath)
{
  return
}

$msBuildPath