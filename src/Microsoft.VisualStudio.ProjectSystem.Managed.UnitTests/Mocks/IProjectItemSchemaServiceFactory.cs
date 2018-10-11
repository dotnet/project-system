// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

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
