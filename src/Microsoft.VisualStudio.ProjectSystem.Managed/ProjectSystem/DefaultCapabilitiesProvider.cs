// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem
{
    // WORKAROUND: See https://github.com/dotnet/roslyn-project-system/issues/559
    [Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectCapabilitiesProvider))]
    [AppliesTo(ProjectCapabilities.AlwaysApplicable)]
    internal sealed class DefaultCapabilitiesProvider : UnconfiguredProjectCapabilitiesProviderBase
    {
        private static readonly ImmutableHashSet<string> CSharpDefaultCapabilities = ImmutableHashSet<string>.Empty.Add(ProjectCapabilities.ProjectConfigurationsInferredFromUsage)
                                                                                                                   .Add(ProjectCapabilities.LanguageService)
                                                                                                                   .Add(ProjectCapabilities.CSharp);

        private static readonly ImmutableHashSet<string> VisualBasicDefaultCapabilities = ImmutableHashSet<string>.Empty.Add(ProjectCapabilities.ProjectConfigurationsInferredFromUsage)
                                                                                                                        .Add(ProjectCapabilities.LanguageService)
                                                                                                                        .Add(ProjectCapabilities.VB);

        private readonly ImmutableHashSet<string> _capabilities;

        [ImportingConstructor]
        public DefaultCapabilitiesProvider(UnconfiguredProject unconfiguredProject)
            : base(typeof(DefaultCapabilitiesProvider).Name, unconfiguredProject)
        {
            var capabilities = ImmutableHashSet<string>.Empty;
            if (unconfiguredProject.FullPath != null)
            {
                if (unconfiguredProject.FullPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                {
                    capabilities = CSharpDefaultCapabilities;
                }
                else if (unconfiguredProject.FullPath.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase))
                {
                    capabilities = VisualBasicDefaultCapabilities;
                }
            }

            _capabilities = capabilities;
        }

        protected override Task<ImmutableHashSet<string>> GetCapabilitiesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_capabilities);
        }
    }
}