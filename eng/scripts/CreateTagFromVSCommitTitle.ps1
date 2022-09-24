# Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

# This creates a tag in our repo (.NET Project System) based on parsing the latest commit title in the VS repo.

param ([Parameter(Mandatory=$true)] [string] $vsDirectory, [Parameter(Mandatory=$true)] [string] $vsCommitId)

Set-Location $vsDirectory

# Gets the subject (title) from the provided commit ID (via vsCommitId). See:
# - https://stackoverflow.com/a/7293026/294804
# - https://git-scm.com/docs/git-log#_pretty_formats
$commitTitle = (git log -1 --pretty=%s $vsCommitId)
# https://stackoverflow.com/a/48877892/294804
if($LastExitCode -ne 0)
{
  Write-Host "Failed to get commit title for VS commit ID: $vsCommitId"
  exit $LastExitCode
}
# Parse the short commit ID out of the commit title. See:
# - https://stackoverflow.com/a/3697210/294804
# - https://stackoverflow.com/a/12001377/294804
# Note: Only including alphanumeric and dot, underscore, and hyphen in the branch name.
# See this for how complex branch names can be:
# - https://stackoverflow.com/a/12093994/294804
# - https://stackoverflow.com/a/3651867/294804
$hasShortCommitId = $commitTitle -match 'DotNet-Project-System \([a-zA-Z0-9._-]+:\d+(\.\d+)*:(\w+)\)'
if($hasShortCommitId)
{
  $shortCommitId = $matches[2]
  # Default to VS repo short commit ID as part of the tag when the commit isn't a merge commit.
  # In almost all cases, vsTagIdentifier will be set to the PR number since we we primarily create merge commits.
  # See also: https://stackoverflow.com/a/21015031/294804
  $vsTagIdentifier = (git log -1 --pretty=%h $vsCommitId)
  $hasPRNumber = $commitTitle -match 'Merged PR (\d+):'
  if($hasPRNumber)
  {
    $prNumber = $matches[1]
    $vsTagIdentifier = "PR-$prNumber"
  }

  $tagName = "insertion/$vsTagIdentifier"
  Set-Location $PSScriptRoot
  # Using a lightweight tag since we don't need any other information than the tag name itself.
  # https://git-scm.com/book/en/v2/Git-Basics-Tagging
  git tag $tagName $shortCommitId
  git push origin $tagName
  # https://stackoverflow.com/a/48877892/294804
  if($LastExitCode -ne 0)
  {
    Write-Host "Failed to create tag for commit ID: $shortCommitId"
    exit $LastExitCode
  }
  exit 0
}

Write-Host "Short commit ID was not found in commit title: $commitTitle"
exit 1