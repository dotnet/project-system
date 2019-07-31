// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    [ExportIVsReferenceManagerUserAsync(VSConstants.ProjectReferenceProvider_string, ReferencePriority.Project)]
    [AppliesTo(ProjectCapability.DotNet)]
    [Order(Order.Default)]
    internal class ProjectReferencesProviderContext : BaseReferenceContextProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyReferencesProviderContext"/> class.
        /// </summary>
        [ImportingConstructor]
        public ProjectReferencesProviderContext(ConfiguredProject configuredProject) : base(configuredProject)
        {
        }

        /// <summary>
        /// Returns a value indicating whether this provider should be activated.
        /// </summary>
        /// <returns>Value indicating whether this provider should be activated.</returns>
        public override bool IsApplicable()
        {
            return ConfiguredProject.Capabilities.AppliesTo(ProjectCapability.ReferenceManagerProjects);
        }
    }
}
