// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.ProjectSystem.Build;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging
{
    [Export(typeof(IBuildLoggerProviderAsync))]
    [AppliesTo(ProjectCapabilities.AlwaysApplicable)]
    internal sealed class BuildLoggerProvider : IBuildLoggerProviderAsync
    {
        private readonly IBuildManager _buildManager;
        private readonly ConfiguredProject _configuredProject;

        [ImportingConstructor]
        public BuildLoggerProvider(IBuildManager buildManager, ConfiguredProject configuredProject)
        {
            _buildManager = buildManager;
            _configuredProject = configuredProject;
        }

        public Task<IImmutableSet<ILogger>> GetLoggersAsync(IReadOnlyList<string> targets, IImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
        {
            var loggers = (IImmutableSet<ILogger>)ImmutableHashSet<ILogger>.Empty;

            if (_buildManager.IsLogging)
            {
                loggers = loggers.Add(new FakeLogger(_buildManager, _configuredProject));
            }

            return Task.FromResult(loggers);
        }
    }
}
