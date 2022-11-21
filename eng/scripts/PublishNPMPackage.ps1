# Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

# Publishes an NPM package. If the package already exists in the registry from the userconfig, it will ignore the error and continue.
# See: https://docs.npmjs.com/cli/v8/commands/npm-publish

param ([Parameter(Mandatory=$true)] [string] $npmrcPath, [Parameter(Mandatory=$true)] [string] $tgzPath)

Write-Host 'Inputs:'
Write-Host "npmrcPath: $npmrcPath"
Write-Host "tgzPath: $tgzPath"

$packageFilename = Split-Path -Path $tgzPath -Leaf
$publishCommand = "npm publish --userconfig ""$npmrcPath"" ""$tgzPath"""
# https://stackoverflow.com/a/56394672/294804
$publishOutput = & cmd /c "$publishCommand 2>&1"
if ($LastExitCode -ne 0)
{
  $isPackageAlreadyPublished = $publishOutput | Where-Object { $_ -like '*The feed*already contains file*in package*' }
  if($isPackageAlreadyPublished)
  {
    Write-Host "The package '$packageFilename' is already published. Skipping..."
    exit 0
  }

  Write-Host "##vso[task.logissue type=error]An error occurred while publishing the package '$packageFilename'."
}

$publishOutput