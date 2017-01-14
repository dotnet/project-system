// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using EnvDTE80;
using Moq;

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
