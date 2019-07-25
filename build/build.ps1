[CmdletBinding(PositionalBinding=$false)]
Param(
  [string] $configuration = "Debug",
  [string] $solution = "",
  [string] $verbosity = "minimal",
  [switch] $restore,
  [switch] $build,
  [switch] $rebuild,
  [switch] $deploy,
  [switch] $test,
  [switch] $integrationTest,
  [string] $rootsuffix = "",
  [switch] $sign,
  [switch] $pack,
  [switch] $ci,
  [switch] $prepareMachine,
  [switch] $ibc,
  [switch] $log,
  [switch] $help,
  [Parameter(ValueFromRemainingArguments=$true)][String[]]$properties
)

set-strictmode -version 2.0
$ErrorActionPreference = "Stop"

function Print-Usage() {
    Write-Host "Common settings:"
    Write-Host "  -configuration <value>  Build configuration Debug, Release"
    Write-Host "  -verbosity <value>      Msbuild verbosity (q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic])"
    Write-Host "  -help                   Print help and exit"
    Write-Host ""

    Write-Host "Actions:"
    Write-Host "  -restore                Restore dependencies"
    Write-Host "  -build                  Build solution"
    Write-Host "  -rebuild                Rebuild solution"
    Write-Host "  -deploy                 Deploy built VSIXes"
    Write-Host "  -test                   Run all unit tests in the solution"
    Write-Host "  -integrationTest        Run all integration tests in the solution"
    Write-Host "  -sign                   Sign build outputs"
    Write-Host "  -pack                   Package build outputs into NuGet packages and Willow components"
    Write-Host ""

    Write-Host "Advanced settings:"
    Write-Host "  -solution <value>       Path to solution to build"
    Write-Host "  -ci                     Set when running on CI server"
    Write-Host "  -log                    Enable logging (by default on CI)"
    Write-Host "  -prepareMachine         Prepare machine for CI run"
    Write-Host "  -ibc                    Enable IBC (OptProf) optimization data usage"
    Write-Host ""
    Write-Host "Command line arguments not listed above are passed thru to msbuild."
    Write-Host "The above arguments can be shortened as much as to be unambiguous (e.g. -co for configuration, -t for test, etc.)."
}

if ($help -or (($properties -ne $null) -and ($properties.Contains("/help") -or $properties.Contains("/?")))) {
  Print-Usage
  exit 0
}

function Create-Directory([string[]] $path) {
  if (!(Test-Path $path)) {
    New-Item -path $path -force -itemType "Directory" | Out-Null
  }
}

function GetVersion([string] $name) {
  foreach ($propertyGroup in $VersionsXml.Project.PropertyGroup) {
    if (Get-Member -inputObject $propertyGroup -name $name) {
        return $propertyGroup.$name
    }
  }

  throw "Failed to find $name in Versions.props"
}


function InstallToolset {
  if ($log) {
    $logCmd = "/bl:" + (Join-Path $LogDir "RestoreToolset.binlog")
  } else {
    $logCmd = ""
  }

  if (!(Test-Path $ToolsetBuildProj)) {
    & $MsbuildExe $ToolsetRestoreProj /t:restore /m /nologo /clp:None /warnaserror /v:quiet /p:NuGetPackageRoot=$NuGetPackageRoot /p:BaseIntermediateOutputPath=$ToolsetDir /p:ExcludeRestorePackageImports=true $logCmd
  }
}

function Build {
  if ($log) {
    $logCmd = "/bl:" + (Join-Path $LogDir "Build.binlog")
  } else {
    $logCmd = ""
  }

  $nodeReuse = !$ci
  $useCodecov = $ci -and $env:CODECOV_TOKEN -and ($configuration -eq 'Debug') -and ($env:ghprbPullAuthorLogin -ne 'dotnet-bot')
  $useOpenCover = $useCodecov

  & $MsbuildExe $ToolsetBuildProj /m /nologo /clp:Summary /nodeReuse:$nodeReuse /warnaserror /v:$verbosity $logCmd /p:Configuration=$configuration /p:SolutionPath=$solution /p:Restore=$restore /p:QuietRestore=true /p:Build=$build /p:Rebuild=$rebuild /p:Deploy=$deploy /p:Test=$test /p:IntegrationTest=$integrationTest /p:Sign=$sign /p:Pack=$pack /p:UseCodecov=$useCodecov /p:UseOpenCover=$useOpenCover /p:CIBuild=$ci /p:EnableIbc=$ibc /p:NuGetPackageRoot=$NuGetPackageRoot $properties
  if ((-not $?) -or ($lastExitCode -ne 0)) {
    throw "Aborting after build failure."
  }

  if ($useCodecov) {
    $CodecovProj = Join-Path $PSScriptRoot 'Codecov.proj'
    & $MsbuildExe $CodecovProj /m /nologo /clp:Summary /nodeReuse:$nodeReuse /warnaserror /v:diag /t:Codecov /p:Configuration=$configuration /p:UseCodecov=$useCodecov /p:NuGetPackageRoot=$NuGetPackageRoot $properties
  }
}

function Stop-Processes() {
  Write-Host "Killing running build processes..."
  Stop-Process-Name "msbuild"
  Stop-Process-Name "vbcscompiler"
}

function Stop-Process-Name([string] $processName) {
  Get-Process -Name $processName -ErrorAction SilentlyContinue | Stop-Process
}

function Clear-NuGetCache() {
  # clean nuget packages -- necessary to avoid mismatching versions of swix microbuild build plugin and VSSDK on Jenkins
  $nugetRoot = (Join-Path $env:USERPROFILE ".nuget\packages")
  if (Test-Path $nugetRoot) {
    Remove-Item $nugetRoot -Recurse -Force
  }
}

function GenerateDependentAssemblyVersionFile() {
  $vsAssemblyName = "Microsoft.VisualStudio.Editors"
  $visualStudioVersion = GetVersion("VisualStudioVersion")
  $projectSystemAssemblyName = "Microsoft.VisualStudio.ProjectSystem.Managed"
  $projectSystemVersion = GetVersion("ProjectSystemVersion")
  $devDivInsertionFiles = Join-Path (Join-Path $ArtifactsDir $configuration) "DevDivInsertionFiles"
  $dependentAssemblyVersionsCsv = Join-Path $devDivInsertionFiles "DependentAssemblyVersions.csv"
  $csv =@"
$vsAssemblyName,$visualStudioVersion.0
$projectSystemAssemblyName,$projectSystemVersion.0
"@
  & mkdir -force $devDivInsertionFiles > $null
  $csv > $dependentAssemblyVersionsCsv
}

try {
  $RepoRoot = Join-Path $PSScriptRoot "..\"
  $ToolsRoot = Join-Path $RepoRoot ".tools"
  $ToolsetRestoreProj = Join-Path $PSScriptRoot "Toolset.proj"
  $ArtifactsDir = Join-Path $RepoRoot "artifacts"
  $ToolsetDir = Join-Path $ArtifactsDir "toolset"
  $LogDir = Join-Path (Join-Path $ArtifactsDir $configuration) "log"
  $BinDir = Join-Path (Join-Path $ArtifactsDir $configuration) "bin"
  $VSSetupDir = Join-Path (Join-Path $ArtifactsDir $configuration) "VSSetup"
  $TestResultsDir = Join-Path (Join-Path $ArtifactsDir $configuration) "TestResults"
  $TempDir = Join-Path (Join-Path $ArtifactsDir $configuration) "tmp"
  [xml]$VersionsXml = Get-Content(Join-Path $PSScriptRoot "Versions.props")

  if ($solution -eq "") {
    $solution = @(gci(Join-Path $RepoRoot "*.sln"))[0]
  }

  if ($env:NUGET_PACKAGES -ne $null) {
    $NuGetPackageRoot = $env:NUGET_PACKAGES.TrimEnd("\") + "\"
  } else {
    $NuGetPackageRoot = Join-Path $env:UserProfile ".nuget\packages\"
  }

  $ToolsetVersion = GetVersion("RoslynToolsRepoToolsetVersion")
  $ToolsetBuildProj = Join-Path $NuGetPackageRoot "RoslynTools.RepoToolset\$ToolsetVersion\tools\Build.proj"

  $vsInstallDir = $env:VSINSTALLDIR
  $MsbuildExe = "msbuild.exe"

  if (($vsInstallDir -eq $null) -or !(Test-Path $vsInstallDir)) {
    throw "This script must be run from a Visual Studio Command Prompt."
  }

  if ($ci) {
    Create-Directory $TempDir
    $env:TEMP = $TempDir
    $env:TMP = $TempDir
  }

  if ($log) {
    # Always create these directories so publish 
    # and writes to these folders succeed
    Create-Directory $LogDir
    Create-Directory $BinDir
    Create-Directory $VSSetupDir
    Create-Directory $TestResultsDir
  }

  # Preparation of a CI machine
  if ($prepareMachine) {
    Clear-NuGetCache
  }

  if ($restore) {
    InstallToolset
  }

  Build

  GenerateDependentAssemblyVersionFile
  
  exit $lastExitCode
}
catch {
  Write-Host $_
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  exit 1
}
finally {
  Pop-Location
  if ($ci -and $prepareMachine) {
    Stop-Processes
  }
}
