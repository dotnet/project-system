// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.Providers
{
    public interface IBuildTableDataSource : ITableDataSource
    {
        ITableManager Manager { get; set; }

        bool IsLogging { get; }

        void Start();

        void Stop();

        void Clear();

        ILogger CreateLogger(bool isDesignTime);
    }
}
