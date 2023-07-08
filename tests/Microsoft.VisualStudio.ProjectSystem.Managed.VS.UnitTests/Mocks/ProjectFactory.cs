// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using EnvDTE80;

namespace EnvDTE
{
    internal static class ProjectFactory
    {
        public static Project Create()
        {
            return Mock.Of<Project>();
        }

        public static Project ImplementObject(Func<object> action)
        {
            var mock = new Mock<Project>();
            mock.SetupGet(p => p.Object)
                .Returns(action);

            return mock.Object;
        }

        public static Project CreateWithSolution(Solution2 solution)
        {
            var mock = new Mock<Project>();

            mock.SetupGet(p => p.DTE.Solution).Returns((Solution)solution);

            return mock.Object;
        }

        internal static void ImplementCodeModelLanguage(Project project, string language)
        {
            var mock = Mock.Get(project);
            mock.SetupGet(p => p.CodeModel.Language).Returns(language);
        }
    }
}
