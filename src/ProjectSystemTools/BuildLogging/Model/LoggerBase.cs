// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model
{
    internal abstract class LoggerBase : ILogger
    {
        protected readonly BuildTableDataSource DataSource;

        public LoggerVerbosity Verbosity { get => LoggerVerbosity.Diagnostic; set { } }

        public string Parameters { get; set; }

        protected LoggerBase(BuildTableDataSource dataSource)
        {
            DataSource = dataSource;
        }

        public abstract void Initialize(IEventSource eventSource);

        public virtual void Shutdown()
        {
        }
    }
}
