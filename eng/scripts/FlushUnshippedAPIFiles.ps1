# Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

# Copies the contents of PublicAPI.Unshipped.txt into PublicAPI.Shipped.txt on build.
# It assumes that the project directory contains these files (PublicApiAnalyzers also makes this assumption).

param ([Parameter(Mandatory=$true)] [string] $projectDirectory)

Write-Host 'Inputs:'
Write-Host "projectDirectory: $projectDirectory"

foreach($unshipped in Get-ChildItem -Recurse -Path $projectDirectory "PublicAPI.Unshipped.txt")
{
  $shipped = $unshipped.FullName.Replace('Unshipped', 'Shipped')
  $content = Get-Content $unshipped.FullName -Raw

  if(-Not [String]::IsNullOrWhiteSpace($content))
  {
    Write-Host "Copying $($unshipped.FullName) to $($shipped.FullName)"
    # Uses AppendAllText instead of Add-Content since that cmdlet adds an empty line at the end of the file.
    [IO.File]::AppendAllText($shipped, [Environment]::NewLine + $content)
    Clear-Content $unshipped.FullName
  }
}
