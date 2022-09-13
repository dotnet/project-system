# Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

$previousSha = '09c9fb467c042c5441ca2cde0742af9d88954a7e'
$currentSha = 'aa3a7acfdc903f398584936976560041d1cf24c6'
# BUILD_REPOSITORY_URI
$repoUrl = 'https://github.com/dotnet/project-system'
# BUILD_DEFINITIONNAME
$projectName = 'DotNet-Project-System'


$previousShaShort = $previousSha.Substring(0,6)
$currentShaShort = $currentSha.Substring(0,6)
# [Environment]::NewLine
$description = @()
$description += "Updating $projectName from ([$previousShaShort]($repoUrl/commit/$previousSha)) to ([$currentShaShort]($repoUrl/commit/$currentSha))"
$description += '---'
# The 'w' query parameter is for ignoring whitespace.
# See: https://stackoverflow.com/a/37145215/294804
$description += "[View Complete Diff of Changes]($repoUrl/compare/$previousSha...$currentSha?w=1)"

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

$commitsJson = $commitsClean | ConvertFrom-Json


# $prNumbers = @()
# $filtered = $commitsJson | Where-Object { $isPr = $_.subject -match "^Merge pull request #(\d+) from"; if($isPr) { $prNumbers += $matches[1] }; $isPr }

# Filter the commits to only PR merges and replace 'subject' with only the PR number.
$pullRequests = $commitsJson | Where-Object { $isPr = $_.subject -match '^Merge pull request #(\d+) from'; if($isPr) { $_.subject = $matches[1] }; $isPr }

$description += $pullRequests | ForEach-Object { "- [($($_.subject)) $($_.body)]($repoUrl/pull/$($_.subject))" } | Out-String

$description | Out-File 'description.md'

# Name  : BUILD_REPOSITORY_URI
# Value : https://github.com/dotnet/project-system


# "$repoUrl/compare/$previousSha...$currentSha?w=1"





# Previously, there were insertion PR descriptions that contained 3 components:
# To-From links for each build
# Link for viewing complete diff of changes
# Links to each PR that is contained within 