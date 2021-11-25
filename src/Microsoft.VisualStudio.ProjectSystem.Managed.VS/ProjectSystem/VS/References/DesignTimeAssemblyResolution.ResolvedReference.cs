// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    internal partial class DesignTimeAssemblyResolution
    {
        private readonly struct ResolvedReference
        {
            public ResolvedReference(string resolvedPath, Version? version)
            {
                Assumes.NotNull(resolvedPath);

                ResolvedPath = resolvedPath;
                Version = version;
            }

            public string ResolvedPath { get; }

            public Version? Version { get; }
        }
    }
}
