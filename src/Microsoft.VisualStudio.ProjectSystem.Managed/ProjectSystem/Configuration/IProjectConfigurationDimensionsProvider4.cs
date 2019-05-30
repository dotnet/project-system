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
