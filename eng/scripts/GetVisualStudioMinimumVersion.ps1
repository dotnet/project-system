# Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

# Gets the major version number from the provided version.json and returns it.

param ([string] $versionJsonPath = "$PSScriptRoot\..\..\version.json")

if(-Not (Test-Path -Path $versionJsonPath))
{
  return
}

$versionJson = Get-Content $versionJsonPath | ConvertFrom-Json
# Use only the Major version value from the version number.
($versionJson.version.Split('.'))[0]