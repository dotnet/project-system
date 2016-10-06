// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private static readonly ImmutableHashSet<string> _capabilities = ImmutableHashSet<string>.Empty.Add(ProjectCapabilities.ProjectConfigurationsInferredFromUsage);

        [ImportingConstructor]
        public ProjectConfigurationsInferredFromUsageCapabilitiesProvider(UnconfiguredProject unconfiguedProject)
            : base(typeof(ProjectConfigurationsInferredFromUsageCapabilitiesProvider).Name, unconfiguedProject)
        {
        }

        protected override Task<ImmutableHashSet<string>> GetCapabilitiesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_capabilities);
        }
    }
}