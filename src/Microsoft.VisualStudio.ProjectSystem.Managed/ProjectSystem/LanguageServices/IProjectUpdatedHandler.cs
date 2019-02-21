// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Logging;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal interface IProjectUpdatedHandler : IWorkspaceContextHandler
    {
        string ProjectUpdatedRule { get; }

        Task HandleUpdateAsync(IComparable version, IProjectChangeDescription projectChange, bool isActiveContext, IProjectLogger logger);
    }
}
