# Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

# Sets the Experimental attribute on the VSIX manifest to 'false' for builds in CI.

param ([Parameter(Mandatory=$true)] [string] $manifestPath)

Write-Host 'Inputs:'
Write-Host "manifestPath: $manifestPath"

$manifestXml = [Xml.XmlDocument](Get-Content $manifestPath)
$manifestXml.PackageManifest.Installation.SetAttribute('Experimental', 'false')
$manifestXml.Save($manifestPath)