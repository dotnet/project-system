# Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

# This extracts the Metadata.json file information from the OptProf artifact in the build and sets the drop name to the PreviousOptimizationInputsDropName variable.
# This is used within the Optimization stage of official.yml.
# See LKG support for details: https://devdiv.visualstudio.com/DevDiv/_wiki/wikis/DevDiv.wiki/29053/Enabling-LKG-support

param ([Parameter(Mandatory=$true)] [string] $buildId)

$artifactParameters = @{
  InstanceURL = 'https://dev.azure.com/devdiv'
  ProjectName = 'DevDiv'
  BuildID = $buildId
  ArtifactName = 'OptProf'
  OAuthAccessToken = (ConvertTo-SecureString $env:SYSTEM_ACCESSTOKEN -AsPlainText -Force)
}
$artifact = Get-BuildArtifact @artifactParameters
$containerName = $artifact.Resource.Data -Split '/' | Select-Object -Last 1
$metadataString = Read-BuildArtifactFile @artifactParameters -FileName (Join-Path $containerName 'Metadata.json')
$dropName = ($metadataString | ConvertFrom-Json).OptimizationData

Write-Host "PreviousOptimizationInputsDropName: $dropName"
Set-AzurePipelinesVariable 'PreviousOptimizationInputsDropName' $dropName