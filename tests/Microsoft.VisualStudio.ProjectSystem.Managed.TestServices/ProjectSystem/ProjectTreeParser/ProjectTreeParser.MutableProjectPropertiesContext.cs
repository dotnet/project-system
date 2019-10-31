// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
