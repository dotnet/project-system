// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;

namespace Microsoft.VisualStudio
{
    internal static class IQueryExecutionContextFactory
    {
        public static IQueryExecutionContext Create(IEntityRuntimeModel? runtimeModel = null)
        {
            var mock = new Mock<IQueryExecutionContext>();

            if (runtimeModel is null)
            {
                runtimeModel = IEntityRuntimeModelFactory.Create();
            }

            mock.SetupGet(m => m.EntityRuntime).Returns(runtimeModel);

            return mock.Object;
        }
    }
}
