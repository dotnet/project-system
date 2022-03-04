# Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

# This updates the provided swixproj/vsmanproj with the necessary MergeManifest node attribute for SBOM metadata to propegate to the VSIX packages.

param ([Parameter(Mandatory=$true)] [String] $projPath)
