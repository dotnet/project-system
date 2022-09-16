# Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

# Creates a description for VS Insertion PRs that contains a list of PRs that have been merged between since the previous VS Insertion.
# The previous VS Insertion relies on finding the last tag in our repo that matches the pattern: VS-Insertion-*
# This script outputs the description string into the InsertionDescription variable within the Azure Pipeline.

param ([Parameter(Mandatory=$true)] [string] $currentSha, [Parameter(Mandatory=$true)] [string] $repoUrl, [Parameter(Mandatory=$true)] [string] $projectName)

# Using 10 characters since that will make it incredibly unlikely that there will be a collision.
# https://stackoverflow.com/a/18134919/294804
$currentShaShort = $currentSha.Substring(0,10)
# This is not using isoutput=true since this variable is only needed within the job itself.
# https://docs.microsoft.com/azure/devops/pipelines/process/set-variables-scripts?view=azure-devops&tabs=powershell#set-an-output-variable-for-use-in-the-same-job
Write-Host "##vso[task.setvariable variable=ShortCommitId]$currentShaShort"

$description = @()
# Gets the commit ID of the latest tag that matches VS-Insertion-*
# https://git-scm.com/docs/git-rev-list
# https://stackoverflow.com/a/1862542/294804
$previousSha = (git rev-list --tags=VS-Insertion-* -1)
# Since a previous commit ID was not found, we create a basic description.
if(-Not $previousSha)
{
  $description += "Updating $projectName to [$currentShaShort]($repoUrl/commit/$currentSha)"
  $description += '---'
  $description += 'Unable to determine the previous VS insertion. PR changelist cannot be computed.'
  # Write-Host "##vso[task.setvariable variable=InsertionDescription]$($description -join '%0D%0A')"
  Write-Host "##vso[task.setvariable variable=InsertionDescription]$($description -join '^`n`r')"
  exit 0
}
$previousShaShort = $previousSha.Substring(0,10)

$description += "Updating $projectName from [$previousShaShort]($repoUrl/commit/$previousSha) to [$currentShaShort]($repoUrl/commit/$currentSha)"
$description += '---'
# The 'w' query parameter is for ignoring whitespace.
# See: https://stackoverflow.com/a/37145215/294804
$description += "Included PRs: ([View Diff]($repoUrl/compare/$previousSha...$currentSha?w=1))"

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
# Replace any double quotes with a backslash double quote.
# This situation occurs when commit bodies or commit subjects contain double quotes in them.
$commitsClean = $commitsClean -replace '"','\"'
# Replace the quadruple carats with double quotes.
$commitsClean = $commitsClean -replace '\^\^\^\^','"'
# Add a comma between the end+start of new commit entries. This ensures the last entry won't have a trailing comma.
# Note that the replacement value is using interpolated string (double quoted string) so the newline characters (`r`n) are rendered in the string.
# See: https://stackoverflow.com/a/39104508/294804
$commitsClean = $commitsClean -replace '\}\r\n\{',"},`r`n{"

# Filter the commits to only PR merges and replace 'subject' with only the PR number.
$pullRequests = $commitsClean | ConvertFrom-Json | Where-Object { $isPr = $_.subject -match '^Merge pull request #(\d+) from'; if($isPr) { $_.subject = $matches[1] }; $isPr }
# Create a markdown list item for each PR.
$description += $pullRequests | ForEach-Object { "- [($($_.subject)) $($_.body)]($repoUrl/pull/$($_.subject))" }

# 4000 character limit is imposed by Azure Pipelines. See:
# https://developercommunity.visualstudio.com/t/raise-the-character-limit-for-pull-request-descrip/365708
# Remove the last line (PR) from the description until it is less than 4000 characters, including the 6 characters for newlines (%0D%0A) (see Write-Host below).
$characterLimit = 4000
$isTruncated = $false
while((($description | Measure-Object -Character).Characters + (($description.Count - 1) * 6)) -gt $characterLimit)
{
  $description = $description | Select-Object -SkipLast 1
  $isTruncated = $true
  # Make room for 9 characters: 3 for ellipsis (...) and 6 characters for the newline (%0D%0A).
  $characterLimit = 3991
}
if($isTruncated)
{
  $description += '...'
}

# Merge the description lines into a single string (using %0D%0A for newline) and set it to InsertionDescription.
# https://developercommunity.visualstudio.com/t/multiple-lines-variable-in-build-and-release/365667
# https://stackoverflow.com/a/49947273/294804
# Write-Host "##vso[task.setvariable variable=InsertionDescription]$($description -join '%0D%0A')"
Write-Host "##vso[task.setvariable variable=InsertionDescription]$($description -join '^`n`r')"