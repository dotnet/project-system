﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore;

internal static class ProjectRestoreInfoFactory
{
    public static ProjectRestoreInfo Create(string? msbuildProjectExtensionsPath = null)
    {
        return new ProjectRestoreInfo(msbuildProjectExtensionsPath ?? string.Empty, string.Empty, string.Empty,
                                      RestoreBuilder.EmptyTargetFrameworks,
                                      RestoreBuilder.EmptyReferences);
    }
}
