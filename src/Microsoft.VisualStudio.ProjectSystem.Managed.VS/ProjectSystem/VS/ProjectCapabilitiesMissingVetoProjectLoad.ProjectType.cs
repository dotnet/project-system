// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal partial class ProjectCapabilitiesMissingVetoProjectLoad
    {
        private class ProjectType
        {
            public ProjectType(string extension, IEnumerable<string> capabilities)
            {
                Extension = extension;
                Capabilities = capabilities.ToImmutableArray();
            }

            public string Extension;
            public ImmutableArray<string> Capabilities;
        }
    }
}
