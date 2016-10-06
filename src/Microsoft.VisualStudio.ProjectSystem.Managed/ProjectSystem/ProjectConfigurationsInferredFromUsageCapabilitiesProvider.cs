// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectCapabilitiesProvider))]
    [AppliesTo(ProjectCapabilities.AlwaysApplicable)]
    internal sealed class ProjectConfigurationsInferredFromUsageCapabilitiesProvider : UnconfiguredProjectCapabilitiesProviderBase
    {
        private readonly ImmutableHashSet<string> _capabilities;

        [ImportingConstructor]
        public ProjectConfigurationsInferredFromUsageCapabilitiesProvider(UnconfiguredProject unconfiguredProject)
            : base(typeof(ProjectConfigurationsInferredFromUsageCapabilitiesProvider).Name, unconfiguredProject)
        {
            var capabilities = ImmutableHashSet<string>.Empty;
            if (unconfiguredProject.FullPath != null &&
                (unconfiguredProject.FullPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                unconfiguredProject.FullPath.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase)))
            {
                capabilities = capabilities.Add(ProjectCapabilities.ProjectConfigurationsInferredFromUsage);
            }

            _capabilities = capabilities;
        }

        protected override Task<ImmutableHashSet<string>> GetCapabilitiesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_capabilities);
        }
    }
}