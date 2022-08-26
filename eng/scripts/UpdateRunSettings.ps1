# Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

# This updates OptProf.runsettings with the bootstrapper information and the profiling inputs path, as TestStore nodes.
# Additionally, sets the visualStudioBootstrapperURI variable in the AzDO pipeline, which is used for the OptProf DartLab template.

param ([Parameter(Mandatory=$true)] [string] $profilingInputsPath, [string] $bootstrapperInfoPath, [string] $buildDropPath)

Write-Host 'Inputs:'
Write-Host "profilingInputsPath: $profilingInputsPath"
Write-Host "bootstrapperInfoPath: $bootstrapperInfoPath"
Write-Host "buildDropPath: $buildDropPath"

$runsettingsPath = (Get-Item "$PSScriptRoot\runsettings\OptProf.runsettings").FullName
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
  # Indicates this script is running in Azure Pipelines.
  # https://docs.microsoft.com/azure/devops/pipelines/build/variables?view=azure-devops&tabs=yaml#system-variables-devops-services
  if($env:TF_BUILD)
  {
    # https://docs.microsoft.com/azure/devops/pipelines/process/set-variables-scripts?view=azure-devops&tabs=powershell#set-variable-properties
    Write-Host "##vso[task.setvariable variable=visualStudioBootstrapperURI;isoutput=true]$($buildDropJson[0].bootstrapperUrl)"
  }
}
$buildDropStore.SetAttribute('Uri', $buildDropPath)
$null = $testStores.AppendChild($buildDropStore)

$runsettingsXml.Save($runsettingsPath)
Write-Host 'Saved Output:'
Write-Host "profilingInputsStore: $($profilingInputsStore.Uri)"
Write-Host "buildDropStore: $($buildDropStore.Uri)"