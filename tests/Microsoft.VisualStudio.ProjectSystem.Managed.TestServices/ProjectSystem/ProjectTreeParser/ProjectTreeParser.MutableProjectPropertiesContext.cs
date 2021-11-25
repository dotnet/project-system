// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal partial class ProjectTreeParser
    {
        private class MutableProjectPropertiesContext : IProjectPropertiesContext
        {
            public bool IsProjectFile => throw new NotImplementedException();

            public string File => throw new NotImplementedException();

            public string ItemType { get; set; } = "";

            public string? ItemName { get; set; }
        }
    }
}
