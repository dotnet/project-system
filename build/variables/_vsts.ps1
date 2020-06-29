Write-Host "List of *.ps1 in PSScriptRoot:" $PSScriptRoot

$RepoRoot = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..")
$ArtifactStagingDirectory = "$RepoRoot\artifacts\"