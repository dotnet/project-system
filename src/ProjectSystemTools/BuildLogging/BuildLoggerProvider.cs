// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging
{
    [Export(typeof(IBuildLoggerProviderAsync))]
    [AppliesTo(ProjectCapabilities.AlwaysApplicable)]
    internal sealed class BuildLoggerProvider : IBuildLoggerProviderAsync
    {
        private readonly IBuildTableDataSource _dataSource;

        [ImportingConstructor]
        public BuildLoggerProvider(IBuildTableDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public Task<IImmutableSet<ILogger>> GetLoggersAsync(IReadOnlyList<string> targets, IImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
        {
            var loggers = (IImmutableSet<ILogger>)ImmutableHashSet<ILogger>.Empty;

            if (_dataSource.IsLogging)
            {
                loggers = loggers.Add(_dataSource);
            }

            return Task.FromResult(loggers);
        }
    }
}
