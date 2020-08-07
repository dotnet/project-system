// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Build.Construction;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal interface IProjectConfigurationDimensionsProvider5 : IProjectConfigurationDimensionsProvider3
    {
        IEnumerable<string> GetBestGuessDimensionNames(ImmutableArray<ProjectPropertyElement> properties);
    }
}
