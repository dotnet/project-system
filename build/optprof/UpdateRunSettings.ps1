# Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

# This updates OptProf.runsettings with the bootstrapper information and the profiling inputs path, as TestStore nodes.

param ([Parameter(Mandatory=$true)] [String] $profilingInputsPath, [String] $bootstrapperInfoPath, [String] $buildDropPath)

Write-Host 'Inputs:'
Write-Host "profilingInputsPath: $profilingInputsPath"
Write-Host "bootstrapperInfoPath: $bootstrapperInfoPath"
Write-Host "buildDropPath: $buildDropPath"

$runsettingsPath = (Get-Item "$PSScriptRoot\OptProf.runsettings").FullName
$runsettingsXml = [Xml.XmlDocument](Get-Content $runsettingsPath)
# https://stackoverflow.com/questions/33813700/empty-xml-node-rendered-as-string-in-powershell
$testStores = $runsettingsXml.RunSettings.TestConfiguration.SelectSingleNode('TestStores')

# https://stackoverflow.com/a/59090765/294804
$profilingInputsStore = $runsettingsXml.CreateElement('TestStore', $runsettingsXml.DocumentElement.NamespaceURI)
$profilingInputsStore.SetAttribute('Uri', "vstsdrop:$profilingInputsPath")
$null = $testStores.AppendChild($profilingInputsStore)

# https://stackoverflow.com/a/59090765/294804
$buildDropStore = $runsettingsXml.CreateElement('TestStore', $runsettingsXml.DocumentElement.NamespaceURI)
if(-not $buildDropPath)
{
  if((-not $bootstrapperInfoPath) -or (-not (Test-Path $bootstrapperInfoPath)))
  {
    Write-Host "Invalid bootstrapperInfoPath: $bootstrapperInfoPath"
    exit -1
  }
  $buildDropJson = Get-Content $bootstrapperInfoPath | ConvertFrom-Json
  $dropHashAndGuid = $buildDropJson[0].BuildDrop.Replace('https://vsdrop.corp.microsoft.com/file/v1/Products/DevDiv/VS/', '')
  $buildDropPath = "vstsdrop:Tests/DevDiv/VS/$dropHashAndGuid"
}
$buildDropStore.SetAttribute('Uri', $buildDropPath)
$null = $testStores.AppendChild($buildDropStore)

$runsettingsXml.Save($runsettingsPath)
Write-Host 'Saved Output:'
Write-Host "profilingInputsStore: $($profilingInputsStore.Uri)"
Write-Host "buildDropStore: $($buildDropStore.Uri)"