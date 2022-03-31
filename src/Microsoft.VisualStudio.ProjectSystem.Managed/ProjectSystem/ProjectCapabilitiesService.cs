// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides an implementation of <see cref="IProjectCapabilitiesService"/> that simply delegates
    ///     onto the <see cref="CapabilitiesExtensions.Contains(IProjectCapabilitiesScope, string)"/> method.
    /// </summary>
    [Export(typeof(IProjectCapabilitiesService))]
    internal class ProjectCapabilitiesService : IProjectCapabilitiesService
    {
        private readonly UnconfiguredProject _project;

        [ImportingConstructor]
        public ProjectCapabilitiesService(UnconfiguredProject project)
        {
            _project = project;
        }

        public bool Contains(string capability)
        {
            Requires.NotNullOrEmpty(capability, nameof(capability));

            // Just to check capabilities, requires static state and call context that we cannot influence
            return _project.Capabilities.Contains(capability);
        }
    }
}
