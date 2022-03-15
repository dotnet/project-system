// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Waiting
{
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IWaitIndicator
    {
        WaitIndicatorResult Run(string title, string message, bool allowCancel, Func<IWaitContext, Task> asyncMethod, int totalSteps = 0);

        WaitIndicatorResult<T> Run<T>(string title, string message, bool allowCancel, Func<IWaitContext, Task<T>> asyncMethod, int totalSteps = 0);
    }
}
