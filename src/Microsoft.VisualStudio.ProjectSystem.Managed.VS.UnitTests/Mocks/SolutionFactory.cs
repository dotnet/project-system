// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using EnvDTE;
using Moq;

namespace EnvDTE80
{
    internal static class SolutionFactory
    {
        public static Solution Create()
        {
            var mock = new Mock<Solution2>();
            return mock.As<Solution>().Object;
        }

        public static Solution2 CreateWithGetProjectItemTemplate(Func<string, string, string> projectItemsTemplatePathFunc)
        {
            var mock = new Mock<Solution2>();
            mock.As<Solution>();
            mock.Setup(s => s.GetProjectItemTemplate(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(projectItemsTemplatePathFunc);

            return mock.Object;
        }

        public static Solution ImplementFindProjectItem(Func<string, ProjectItem> callback)
        {
            var mock = new Mock<Solution>();
            mock.As<Solution2>();
            mock.Setup(m => m.FindProjectItem(It.IsAny<string>())).Returns(callback);
            return mock.Object;
        }
    }
}
