// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem
{
    // There are C#/VB components inside Core CPS that we need to disable - these are conditioned on !ManagedLang,
    // so that we can disable when we are present. Once we're in the install, we remove this and C#/VB components 
    // from CPS itself. Bug tracking this: https://github.com/dotnet/roslyn/issues/11137
    // 
    [Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectCapabilitiesProvider))]
    [AppliesTo(ProjectCapabilities.AlwaysApplicable)]
    internal class DisableInboxComponentsProjectCapabilityProvider : UnconfiguredProjectCapabilitiesProviderBase
    {
        private static readonly ImmutableHashSet<string> _capabilities = ImmutableHashSet<string>.Empty.Add("ManagedLang");

        [ImportingConstructor]
        public DisableInboxComponentsProjectCapabilityProvider(UnconfiguredProject unconfiguedProject)
            : base(typeof(DisableInboxComponentsProjectCapabilityProvider).Name, unconfiguedProject)
        {
        }

        protected override Task<ImmutableHashSet<string>> GetCapabilitiesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_capabilities);
        }
    }
}
