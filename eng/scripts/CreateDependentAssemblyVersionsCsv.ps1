# Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

# Generates DependentAssemblyVersions.csv that enables the Roslyn insertion tool to update various assembly versions in the VS repository under src\ProductData.
# This is used in the RoslynInsertionTool in the file RoslynInsertionTool.AssemblyVersions.cs.
# See: https://github.com/dotnet/roslyn-tools/tree/main/src/RoslynInsertionTool

param ([Parameter(Mandatory=$true)] [String] $csvPath, [Parameter(Mandatory=$true)] [String] $version)

Write-Host 'Inputs:'
Write-Host "csvPath: $csvPath"
Write-Host "version: $version"

# We use individual version for AppDesigner/Editors, however Managed and Managed.VS share the same variable in VS repository under src\ProductData\AssemblyVersions.tt.
$content = "Microsoft.VisualStudio.AppDesigner,$version","Microsoft.VisualStudio.Editors,$version","Microsoft.VisualStudio.ProjectSystem.Managed,$version"
New-Item -Path $csvPath -ItemType File -Force | Set-Content -Value $content