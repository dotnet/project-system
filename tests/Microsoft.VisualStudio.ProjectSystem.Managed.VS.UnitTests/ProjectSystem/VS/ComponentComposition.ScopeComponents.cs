// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable VSMEF003 // Exported type not implemented by exporting class

using ImportAttribute = System.Composition.ImportAttribute;
using ExportAttribute = System.Composition.ExportAttribute;
using SharedAttribute = System.Composition.SharedAttribute;
using SharingBoundaryAttribute = System.Composition.SharingBoundaryAttribute;

namespace Microsoft.VisualStudio.ProjectSystem.VS;

internal partial class ComponentComposition
{
    // These components solely exist so that the MEF composition for 
    // these tests can see the "scopes" used within CPS.

    [Export]
    private class GlobalScope
    {
        [Import]
        [SharingBoundary(ExportContractNames.Scopes.ProjectService)]
        private System.Composition.ExportFactory<IProjectService> ProjectServiceFactory { get; set; } = null!;
    }

    [Export(typeof(IProjectService))]
    [Shared(ExportContractNames.Scopes.ProjectService)]
    private class ProjectServiceScope
    {
        [Import]
        [SharingBoundary(ExportContractNames.Scopes.UnconfiguredProject)]
        private ExportFactory<UnconfiguredProject> UnconfiguredProjectFactory { get; set; } = null!;
    }

    [Export(typeof(UnconfiguredProject))]
    [Shared(ExportContractNames.Scopes.UnconfiguredProject)]
    private class UnconfiguredProjectScope
    {
        [Import]
        [SharingBoundary(ExportContractNames.Scopes.ConfiguredProject)]
        private ExportFactory<ConfiguredProject> ConfiguredProjectFactory { get; set; } = null!;
    }

    [Export(typeof(ConfiguredProject))]
    [Shared(ExportContractNames.Scopes.ConfiguredProject)]
    private class ConfiguredProjectScope
    {
    }
}
