// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.ProjectSystem.Logging;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal interface IProjectUpdatedHandler : IWorkspaceContextHandler
    {
        /// <summary>
        ///     Gets the project evaluation rule that the <see cref="IProjectEvaluationHandler"/> handles.
        /// </summary>
        string ProjectEvaluationRule
        {
            get;
        }

        void HandleProjectUpdate(IComparable version, IProjectChangeDescription projectChange, bool isActiveContext, IProjectLogger logger);
    }
}
