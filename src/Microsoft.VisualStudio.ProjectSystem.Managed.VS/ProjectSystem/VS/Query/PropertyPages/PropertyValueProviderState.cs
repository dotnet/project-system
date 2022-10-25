// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Query.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Holds the state we need to pass from producers of <see cref="UIPropertyValueSnapshot"/> instances
    /// to other producers that will create the <see cref="UIPropertyValueSnapshot"/>s' child entities.
    /// </summary>
    internal sealed class PropertyValueProviderState
    {
        public PropertyValueProviderState(ProjectConfiguration projectConfiguration, ProjectSystem.Properties.IProperty property)
        {
            ProjectConfiguration = projectConfiguration;
            Property = property;
        }

        public ProjectConfiguration ProjectConfiguration { get; }
        public ProjectSystem.Properties.IProperty Property { get; }
    }
}
