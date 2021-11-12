param ([Parameter(Mandatory=$true)] [String] $profilingInputsPath, [String] $bootstrapperInfoPath, [String] $buildDropPath)

$runsettingsPath = (Get-Item 'OptProf.runsettings').FullName
$runsettingsXml = [Xml.XmlDocument](Get-Content $runsettingsPath)
$testStores = $runsettingsXml.RunSettings.TestConfiguration.TestStores

$profilingInputsStore = $runsettingsXml.CreateElement('TestStore')
$profilingInputsStore.SetAttribute('Uri', $profilingInputsPath)
$testStores.AppendChild($profilingInputsStore)

$buildDropStore = $runsettingsXml.CreateElement('TestStore')
if(-not $buildDropPath)
{
  if(-not (Test-Path $bootstrapperInfoPath))
  {
    Write-Host "Invalid bootstrapperInfoPath: $bootstrapperInfoPath"
    exit -1
  }
  $buildDropJson = Get-Content $bootstrapperInfoPath | ConvertFrom-Json
  $dropHashAndGuid = $buildDropJson[0].BuildDrop.TrimStart('https://vsdrop.corp.microsoft.com/file/v1/Products/DevDiv/VS/')
  $buildDropPath = "Tests/DevDiv/VS/$dropHashAndGuid"
}
$buildDropStore.SetAttribute('Uri', $buildDropPath)
$testStores.AppendChild($buildDropStore)

$runsettingsXml.Save($runsettingsPath)