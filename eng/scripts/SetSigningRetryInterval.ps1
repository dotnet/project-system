# Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

# Sets the interval (in seconds) that signing will probe the status for signing completion. Default is 15 seconds.

param ([Parameter(Mandatory=$true)] [string] $signConfigPath, [int] $intervalInSeconds = 15)

Write-Host 'Inputs:'
Write-Host "signConfigPath: $signConfigPath"
Write-Host "intervalInSeconds: $intervalInSeconds"

$signConfigXml = [Xml.XmlDocument](Get-Content $signConfigPath)
$retryInterval = $signConfigXml.MicroBuildSignConfigSettings.SelectSingleNode('PendingJobRetryIntervalInSeconds')
$retryInterval.InnerText = $intervalInSeconds
$signConfigXml.Save($signConfigPath)