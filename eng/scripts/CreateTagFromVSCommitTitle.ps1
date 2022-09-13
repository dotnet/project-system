# Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

# This updates OptProf.runsettings with the bootstrapper information and the profiling inputs path, as TestStore nodes.
# Additionally, sets the visualStudioBootstrapperURI variable in the AzDO pipeline, which is used for the OptProf DartLab template.

param ([Parameter(Mandatory=$true)] [string] $projectSystemDirectory, [Parameter(Mandatory=$true)] [string] $vsDirectory)

Set-Location $vsDirectory
# Gets the subject (title) from the latest commit.
# See:
# - https://stackoverflow.com/a/7293026/294804
# - https://git-scm.com/docs/git-log#_pretty_formats
$commitTitle = (git log -1 --pretty=%s)
# Parse the short commit ID out of the commit title.
# See: https://stackoverflow.com/a/12001377/294804
$hasShortCommitId = $commitTitle -match 'DotNet-Project-System \(\w+:\d+(\.\d+)*:(\w+)\)'
if($hasShortCommitId)
{
  $shortCommitId = $matches[2]
  Set-Location $projectSystemDirectory
  # # Gets the full commit ID from the short commit ID.
  # # See: https://stackoverflow.com/a/41717108/294804
  # $commitId = (git rev-parse $shortCommitId)

  $tagName = "TODO"
  # https://git-scm.com/book/en/v2/Git-Basics-Tagging
  git tag -a $tagName $shortCommitId
  git push origin $tagName
  exit 0
}

Write-Host "Short commit ID was not found in commit title: $commitTitle"
exit 1