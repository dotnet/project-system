// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Build.Construction;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal interface IProjectConfigurationDimensionsProvider4 : IProjectConfigurationDimensionsProvider3
    {
        IEnumerable<string> GetBestGuessDimensionNames(ImmutableArray<ProjectPropertyElement> properties);
    }
}
