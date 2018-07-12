// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Build.Evaluation;

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class ProjectItemFactory
    {
        public static ProjectItem WithValue(string evaluatedInclude, (string name, string value) metadata)
        {
            var mock = new Mock<ProjectItem>();

            mock.SetupGet(p => p.EvaluatedInclude)
                .Returns(evaluatedInclude);

            mock.Setup(p => p.GetMetadataValue(It.Is<string>((t) => string.Equals(t, metadata.name))))
                .Returns(metadata.value);

            return mock.Object;
        }
    }
}
