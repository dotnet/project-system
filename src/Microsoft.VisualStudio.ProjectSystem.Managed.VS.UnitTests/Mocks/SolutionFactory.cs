// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using EnvDTE;
using EnvDTE80;
using Moq;

namespace Microsoft.VisualStudio.Mocks
{
    internal static class SolutionFactory
    {
        public static Solution CreateWithGetProjectItemTemplate(Func<string, string, string> projectItemsTemplatePathFunc)
        {
            var mock = new Mock<Solution>();

            mock.As<Solution2>()
                .Setup(s => s.GetProjectItemTemplate(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(projectItemsTemplatePathFunc);

            return mock.Object;
        }
    }
}
