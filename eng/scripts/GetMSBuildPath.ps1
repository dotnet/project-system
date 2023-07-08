# Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

# Gets the path to MSBuild via vswhere.exe. Returns nothing if a suitable Visual Studio version is not installed.

param ([string] $version)

# https://github.com/microsoft/vswhere/wiki/Installing
$installerPath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer"
if(-Not (Test-Path -Path $installerPath))
{
  return
}

# If the version is not provided, use the version fron the version.json at the root of the repo.
if(-Not $version)
{
  $version = "$(. "$PSScriptRoot\GetVisualStudioMinimumVersion.ps1").0"
}

# This only identifies Visual Studio installations since we do not use the '-products *' parameter. Otherwise, it would also include Visual Studio Build Tools.
# See: https://github.com/microsoft/vswhere/wiki/FAQ#why-doesnt-vswhere-find-visual-studio-build-tools
(& "$installerPath\vswhere.exe" -all -prerelease -latest -version $version -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe) | Select-Object -First 1