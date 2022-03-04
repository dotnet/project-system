# Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

# This updates the provided swixproj/vsmanproj with the necessary MergeManifest node attribute for SBOM metadata to propegate to the VSIX packages.

param ([Parameter(Mandatory=$true)] [String] $projPath, [Parameter(Mandatory=$true)] [String] $manifestDirPath, [Parameter(Mandatory=$true)] [String] $destinationDirPath)

Write-Host 'Inputs:'
Write-Host "projPath: $projPath"
Write-Host "manifestDirPath: $manifestDirPath"
Write-Host "destinationDirPath: $destinationDirPath"

$projXml = [Xml.XmlDocument](Get-Content $projPath)
$manifestFile = 'spdx_2.2\manifest.spdx.json'
$projExtension = [IO.Path]::GetExtension($projPath)

if($projExtension -eq '.swixproj')
{
    $itemGroup = $projXml.Project.ItemGroup[1]
    # https://stackoverflow.com/a/59090765/294804
    $mergeManifest = $projXml.CreateElement('MergeManifest', $projXml.DocumentElement.NamespaceURI)
    $mergeManifest.SetAttribute('Include', '')
    $mergeManifest.SetAttribute('SBOMFileLocation', "$manifestDirPath\$manifestFile")
    $mergeManifest.SetAttribute('SBOMFileDestPath', $destinationDirPath)
    $null = $itemGroup.AppendChild($mergeManifest)
}

if($projExtension -eq '.vsmanproj')
{
    $mergeManifest = $projXml.Project.ItemGroup.MergeManifest
    $mergeManifest.SetAttribute('SBOMFileLocation', "$manifestDirPath\$manifestFile")
    $mergeManifest.SetAttribute('SBOMFileDestPath', $destinationDirPath)
}

$projXml.Save($projPath)