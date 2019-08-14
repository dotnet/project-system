// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#pragma warning disable IDE0051 // Remove unused private members

using System.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal partial class ComponentComposition
    {
        // These components solely exist so that the MEF composition for 
        // these tests can see the "scopes" that used within CPS.

        [Export]
        private class GlobalScope
        {
            [Import]
            [SharingBoundary(ExportContractNames.Scopes.ProjectService)]
            private ExportFactory<IProjectService>? ProjectServiceFactory { get; set; }
        }

        [Export(typeof(IProjectService))]
        [Shared(ExportContractNames.Scopes.ProjectService)]
        private class ProjectServiceScope
        {
            [Import]
            [SharingBoundary(ExportContractNames.Scopes.UnconfiguredProject)]
            private ExportFactory<UnconfiguredProject>? UnconfiguredProjectFactory { get; set; }
        }

        [Export(typeof(ProjectSystem.UnconfiguredProject))]
        [Shared(ExportContractNames.Scopes.UnconfiguredProject)]
        private class UnconfiguredProjectScope
        {
            [Import]
            [SharingBoundary(ExportContractNames.Scopes.ConfiguredProject)]
            private ExportFactory<ProjectSystem.ConfiguredProject>? ConfiguredProjectFactory { get; set; }
        }

        [Export(typeof(ProjectSystem.ConfiguredProject))]
        [Shared(ExportContractNames.Scopes.ConfiguredProject)]
        private class ConfiguredProjectScope
        {
        }
    }
}
