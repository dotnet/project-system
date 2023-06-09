# Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

# Gets the path to npm in Helix agent via powershell search.

$arcadeToolsPath = "${env:SYSTEMDRIVE}\arcade-tools"
$nodeFolderPath = Get-ChildItem $arcadeToolsPath -Filter "node-*" -Directory | ForEach-Object { $_.fullname } | Select-Object -Last 1 
Write-Host "##vso[task.prependpath]$nodeFolderPath"