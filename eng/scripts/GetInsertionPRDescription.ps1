# Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

# Creates a description for VS Insertion PRs that contains a list of PRs that have been merged between since the previous VS Insertion.
# The previous VS Insertion finds the last commit title in the VS repo that matches the pattern: Insert $projectName
# This script outputs the description string into the InsertionDescription variable within the Azure Pipeline.

param ([Parameter(Mandatory=$true)] [string] $vsDirectory, [Parameter(Mandatory=$true)] [string] $currentSha, [Parameter(Mandatory=$true)] [string] $repoUrl, [Parameter(Mandatory=$true)] [string] $projectName)

# Using 10 characters since that will make it incredibly unlikely that there will be a collision.
# https://stackoverflow.com/a/18134919/294804
$currentShaShort = $currentSha.Substring(0,10)
# This is not using isoutput=true since this variable is only needed within the job itself.
# https://docs.microsoft.com/azure/devops/pipelines/process/set-variables-scripts?view=azure-devops&tabs=powershell#set-an-output-variable-for-use-in-the-same-job
Write-Host "##vso[task.setvariable variable=ShortCommitId]$currentShaShort"

Set-Location $vsDirectory
# Gets the commit ID of the latest insertion commit that matches "Insert $projectName"
# --all is required as the fetched Git history may not be the current branch.
# https://git-scm.com/docs/git-rev-list
# https://stackoverflow.com/a/1862542/294804
$vsCommitId = (git rev-list --grep="Insert $projectName" --all -1)
# Gets the subject (title) from the provided commit ID (via vsCommitId). See:
# - https://stackoverflow.com/a/7293026/294804
# - https://git-scm.com/docs/git-log#_pretty_formats
$vsCommitTitle = (git log -1 --pretty=%s $vsCommitId)
# If the VS commit ID wasn't found, it'll be empty, causing the above command to throw an error.
# https://stackoverflow.com/a/48877892/294804
if($LastExitCode -eq 0)
{
  # Parse the short commit ID out of the commit title. See:
  # - https://stackoverflow.com/a/3697210/294804
  # - https://stackoverflow.com/a/12001377/294804
  # Note: Only including alphanumeric and dot, underscore, and hyphen in the branch name.
  # See this for how complex branch names can be:
  # - https://stackoverflow.com/a/12093994/294804
  # - https://stackoverflow.com/a/3651867/294804
  $hasPreviousShaShort = $vsCommitTitle -match "$projectName \([a-zA-Z0-9._-]+:\d+(\.\d+)*:(\w+)\)"
  if($hasPreviousShaShort)
  {
    $previousShaShort = $matches[2]
  }
}

Write-Host "previousShaShort: $previousShaShort"
Write-Host "currentSha: $currentSha"

$description = @()
# Since a previous insertion commit ID was not found, we create a basic description.
if(-Not $previousShaShort)
{
  $description += "Updating $projectName to [$currentShaShort]($repoUrl/commit/$currentSha)"
  $description += '---------------------------------------------------------------------------'
  $description += 'Unable to determine the previous VS insertion.'
  $description += ''
  $description += 'PR changelist cannot be computed.'
  Write-Host "=== DESCRIPTION ===$([Environment]::Newline)$([Environment]::Newline)$($description -join [Environment]::Newline)"
  Write-Host "##vso[task.setvariable variable=InsertionDescription]$($description -join '<br>')"
  exit 0
}

Set-Location $PSScriptRoot
# https://stackoverflow.com/a/41717108/294804
$previousSha = (git rev-parse $previousShaShort)

$description += "Updating $projectName from [$previousShaShort]($repoUrl/commit/$previousSha) to [$currentShaShort]($repoUrl/commit/$currentSha)"
$description += '---------------------------------------------------------------------------'
# The 'w' query parameter is for ignoring whitespace.
# See: https://stackoverflow.com/a/37145215/294804
$description += "Included PRs: [View Diff]($repoUrl/compare/$previousSha...$($currentSha)?w=1)"
$description += ''

# Using quadruple carats as double quotes.
# See: https://gist.github.com/varemenos/e95c2e098e657c7688fd?permalink_comment_id=2856549#gistcomment-2856549
$logFormat = 'format:{ ^^^^subject^^^^: ^^^^%s^^^^, ^^^^body^^^^: ^^^^%b^^^^ }'
# Log command information: https://git-scm.com/docs/git-log
# Pretty format information: https://git-scm.com/docs/pretty-formats
$commits = (git log --pretty="$logFormat" "$previousSha..$currentSha")
# Each commit entry is a single JSON object. Wrap in square brackets to make it an array and merge into single string.
$commitsString = "[ $($commits | Out-String) ]"

# Perform a negative lookbehind for end-curly-bracket so we only find newlines that aren't new commit entries.
# Replace the non-commit entry newline with a space.
# This situation occurs when the commit body contains newlines in it (multiple paragraphs).
$commitsClean = $commitsString -replace '(?<!\})\r\n',' '
# Any remaining backslashes need to be escaped as they are part of the commit body/subject.
# This ignores the natural newlines for the commit entries.
$commitsClean = $commitsClean -replace '(?!\r\n)\\','\\'
# Replace any double quotes with a backslash double quote.
# This situation occurs when commit bodies or commit subjects contain double quotes in them.
$commitsClean = $commitsClean -replace '"','\"'
# Replace the quadruple carats with double quotes.
$commitsClean = $commitsClean -replace '\^\^\^\^','"'
# Add a comma between the end+start of new commit entries. This ensures the last entry won't have a trailing comma.
# Note that the replacement value is using interpolated string (double quoted string) so the newline characters (`r`n) are rendered in the string.
# See: https://stackoverflow.com/a/39104508/294804
$commitsClean = $commitsClean -replace '\}\r\n\{',"},`r`n{"
$commitsJson = $commitsClean | ConvertFrom-Json

# Filter the commits to only PR merges and replace 'subject' with only the PR number.
# 'return' acts like a 'continue' in a script block. See: https://stackoverflow.com/a/7763698/294804
$pullRequests = $commitsJson | Where-Object {
  # Matches the title for a PR merge commit.
  if($_.subject -match '^Merge pull request #(\d+) from')
  {
    $_.subject = $matches[1]
    return $true
  }
  # Matches the title for a PR 'squash and merge' commit.
  if($_.subject -match '^(.*) \(#(\d+)\)$')
  {
    # 'squash and merge' commits do not have a body. Create the body from the subject.
    $_.body = $matches[1]
    $_.subject = $matches[2]
    return $true
  }

  return $false
}
# Create a markdown list item for each PR.
$description += $pullRequests | ForEach-Object { "- [$($_.subject): $($_.body)]($repoUrl/pull/$($_.subject))" }

# 4000 character limit is imposed by Azure Pipelines. See:
# https://developercommunity.visualstudio.com/t/raise-the-character-limit-for-pull-request-descrip/365708
# Remove the last line (PR) from the description until it is less than 4000 characters, including the 4 characters for newlines (<br>) (see InsertionDescription output below).
$characterLimit = 4000
$isTruncated = $false
while((($description | Measure-Object -Character).Characters + (($description.Count - 1) * 4)) -gt $characterLimit)
{
  $description = $description | Select-Object -SkipLast 1
  $isTruncated = $true
  # Make room for 7 characters: 3 for ellipsis (...) and 4 characters for the newline (<br>).
  $characterLimit = 3993
}
if($isTruncated)
{
  $description += '...'
}

# Write out the description into the log for human consumption.
Write-Host "=== DESCRIPTION ===$([Environment]::Newline)$([Environment]::Newline)$($description -join [Environment]::Newline)"

# Merge the description lines into a single string (using <br> for newline) and set it to InsertionDescription. See:
# - https://www.markdownguide.org/basic-syntax/#line-breaks
# - https://learn.microsoft.com/azure/devops/project/wiki/markdown-guidance?view=azure-devops
# Note: We cannot render newlines directly as they are restricted by the PowerShell cmdlet that MicroBuild calls.
# Since we cannot render newlines in the description, we cannot actually render any markdown elements that require newlines (such as lists, horizontal rules, etc.).
# See: https://github.com/microsoft/azure-pipelines-task-lib/blob/ee582bc9b6c7ed2749929b0d66bc34af831e0eea/powershell/VstsTaskSdk/ToolFunctions.ps1#L84-L86
# If we could render newlines, this is how we would need to do it in an Azure Pipelines variable (via '%0D%0A'). See:
# - https://developercommunity.visualstudio.com/t/multiple-lines-variable-in-build-and-release/365667
# - https://stackoverflow.com/a/49947273/294804
Write-Host "##vso[task.setvariable variable=InsertionDescription]$($description -join '<br>')"