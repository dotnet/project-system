// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

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

            public string ResolvedPath
            {
                get;
            }

            public Version? Version
            {
                get;
            }
        }
    }
}
