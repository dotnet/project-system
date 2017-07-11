[CmdletBinding(PositionalBinding=$false)]
Param(
  [string] $configuration = "Debug",
  [string] $solution = "",
  [string] $verbosity = "minimal",
  [switch] $restore,
  [switch] $deployDeps,
  [switch] $build,
  [switch] $rebuild,
  [switch] $deploy,
  [switch] $test,
  [switch] $integrationTest,
  [switch] $sign,
  [switch] $pack,
  [switch] $ci,
  [switch] $prepareMachine,
  [Parameter(ValueFromRemainingArguments=$true)][String[]]$properties
)

set-strictmode -version 2.0
$ErrorActionPreference = "Stop"

$RepoRoot = Join-Path $PSScriptRoot "..\"
$ToolsRoot = Join-Path $RepoRoot ".tools"
$BuildProj = Join-Path $PSScriptRoot "build.proj"
$DependenciesProps = Join-Path $PSScriptRoot "Versions.props"
$ArtifactsDir = Join-Path $RepoRoot "artifacts"
$LogDir = Join-Path (Join-Path $ArtifactsDir $configuration) "log"
$TempDir = Join-Path (Join-Path $ArtifactsDir $configuration) "tmp"

function Create-Directory([string[]] $path) {
  if (!(Test-Path -path $path)) {
    New-Item -path $path -force -itemType "Directory" | Out-Null
  }
}

function GetVSWhereVersion {
  [xml]$xml = Get-Content $DependenciesProps
  return $xml.Project.PropertyGroup.VSWhereVersion
}

function LocateMsbuild {
  
  $vswhereVersion = GetVSWhereVersion
  $vsWhereDir = Join-Path $ToolsRoot "vswhere\$vswhereVersion"
  $vsWhereExe = Join-Path $vsWhereDir "vswhere.exe"
  
  if (!(Test-Path $vsWhereExe)) {
    Create-Directory $vsWhereDir   
    Invoke-WebRequest "http://github.com/Microsoft/vswhere/releases/download/$vswhereVersion/vswhere.exe" -OutFile $vswhereExe
  }
  
  $vsInstallDir = & $vsWhereExe -latest -property installationPath -requires Microsoft.Component.MSBuild -requires Microsoft.VisualStudio.Component.VSSDK -requires Microsoft.Net.Component.4.6.TargetingPack -requires Microsoft.VisualStudio.Component.Roslyn.Compiler -requires Microsoft.VisualStudio.Component.VSSDK
  $msbuildExe = Join-Path $vsInstallDir "MSBuild\15.0\Bin\msbuild.exe"
  
  if (!(Test-Path $msbuildExe)) {
    throw "Failed to locate msbuild (exit code '$lastExitCode')."
  }

  return $msbuildExe
}

function Build {
  $msbuildExe = LocateMsbuild
  
  if ($ci) {
    Create-Directory($logDir)
    # Microbuild is on 15.1 which doesn't support binary log
    if ($env:BUILD_BUILDNUMBER -eq "") {
      $log = "/bl:" + (Join-Path $LogDir "Build.binlog")
    } else {
      $log = "/flp1:Summary;Verbosity=diagnostic;Encoding=UTF-8;LogFile=" + (Join-Path $LogDir "Build.log")
    }
  } else {
    $log = ""
  }

  & $msbuildExe $BuildProj /m /v:$verbosity $log /p:Configuration=$configuration /p:SolutionPath=$solution /p:Restore=$restore /p:DeployDeps=$deployDeps /p:Build=$build /p:Rebuild=$rebuild /p:Deploy=$deploy /p:Test=$test /p:IntegrationTest=$integrationTest /p:Sign=$sign /p:Pack=$pack /p:CIBuild=$ci $properties

  if ($lastExitCode -ne 0) {
    throw "Build failed (exit code '$lastExitCode')."
  }
}

if ($ci) {
  Create-Directory $TempDir
  $env:TEMP = $TempDir
  $env:TMP = $TempDir
}

# Preparation of a CI machine
if ($ci -and $restore -and $prepareMachine) {
  $env:VS150COMNTOOLS = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\\Preview\\Enterprise\\Common7\\Tools\\"
  $env:VSSDK150Install = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\\Preview\\Enterprise\\VSSDK\\"
  $env:VSSDKInstall = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\\Preview\\Enterprise\\VSSDK\\"

  # clean nuget packages -- necessary to avoid mismatching versions of swix microbuild build plugin and VSSDK on Jenkins
  $nugetRoot = (Join-Path $env:USERPROFILE ".nuget\packages")
  if ( -and (Test-Path $nugetRoot)) {
    Remove-Item $nugetRoot -Recurse -Force
  }

  # Install dotnet cli for integration tests
  if ($integrationTest) {
    $dotnetCliSetupExe = Join-Path $TempDir "dotnet-dev-win-x64.1.0.4.exe"
    $dotnetCliSetupLog = Join-Path $LogDir "cli_install.log"

    Invoke-WebRequest "https://download.microsoft.com/download/B/9/F/B9F1AF57-C14A-4670-9973-CDF47209B5BF/dotnet-dev-win-x64.1.0.4.exe" -OutFile $dotnetCliSetup

    & $dotnetCliSetupExe /install /quiet /norestart /log $dotnetCliSetupLog

    if ($lastExitCode -ne 0) {
      throw "Dotnet CLI setup failed (exit code '$lastExitCode')."
    }
  }
}

Build
