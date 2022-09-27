# Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

# Copies the contents of PublicAPI.Unshipped.txt into PublicAPI.Shipped.txt on build.
# It assumes that the project directory contains these files (PublicApiAnalyzers also makes this assumption).

param ([Parameter(Mandatory=$true)] [string] $projectDirectory)

Write-Host 'Inputs:'
Write-Host "projectDirectory: $projectDirectory"

$unshipped = "$projectDirectory\PublicAPI.Unshipped.txt"
$content = Get-Content $unshipped -Raw

if(-Not [String]::IsNullOrWhiteSpace($content))
{
  # Uses AppendAllText instead of Add-Content since that cmdlet adds an empty line at the end of the file.
  [IO.File]::AppendAllText("$projectDirectory\PublicAPI.Shipped.txt", [Environment]::NewLine + $content)
  Clear-Content $unshipped
}