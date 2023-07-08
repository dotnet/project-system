// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using EnvDTE;

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
