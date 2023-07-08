// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectItemSchemaServiceFactory
    {
        public static IProjectItemSchemaService Create()
        {
            var projectItemSchemaService = new Mock<IProjectItemSchemaService>();

            projectItemSchemaService.SetupGet(o => o.SourceBlock)
                .Returns(DataflowBlockSlim.CreateBroadcastBlock<IProjectVersionedValue<IProjectItemSchema>>());

            return projectItemSchemaService.Object;
        }
    }
}
