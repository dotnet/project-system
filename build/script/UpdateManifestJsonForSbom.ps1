# Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

# This updates the provided manifest JSON with the SBOM metadata file. The manifest JSON is used to create the .vsman file for VS insertions.

param ([Parameter(Mandatory=$true)] [String] $manifestJsonPath, [Parameter(Mandatory=$true)] [String] $sbomMetadataPath)

$ErrorActionPreference = 'Stop'

Write-Host 'Inputs:'
Write-Host "manifestJsonPath: $manifestJsonPath"
Write-Host "sbomMetadataPath: $sbomMetadataPath"

$manifestJson = Get-Content $manifestJsonPath | ConvertFrom-Json
$vsixPackageName = [IO.Path]::GetFileNameWithoutExtension($manifestJson.packages[0].payloads[0].fileName)
$newSbomFileName = "$($vsixPackageName)_sbom.json"
# https://stackoverflow.com/a/48601321/294804
$sbomPackageObject = [PSCustomObject]@{
  'fileName' = $newSbomFileName
  'size' = (Get-Item $sbomMetadataPath).Length
}
$manifestJson.packages[0].payloads += $sbomPackageObject
$manifestJson | ConvertTo-Json -Depth 5 | Set-Content $manifestJsonPath

$destinationDir = [IO.Path]::GetDirectoryName($manifestJsonPath)
$newSbomMetadataPath = "$destinationDir\$newSbomFileName"
Copy-Item -Path $sbomMetadataPath -Destination $newSbomMetadataPath

Write-Host 'Saved Output:'
Write-Host "newSbomMetadataPath: $newSbomMetadataPath"