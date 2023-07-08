// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using EnvDTE;

namespace VSLangProj
{
    internal static class VSProjectFactory
    {
        public static VSProject ImplementReferences(IEnumerable<Reference> references)
        {
            var vsProjectReferences = new VSProjectReferences(references);

            var mock = new Mock<VSProject>();
            mock.SetupGet(p => p.References)
                .Returns(vsProjectReferences);

            return mock.Object;
        }

        private class VSProjectReferences : References
        {
            private readonly IEnumerable<Reference> _references;

            public VSProjectReferences(IEnumerable<Reference> references)
            {
                _references = references;
            }

            public IEnumerator GetEnumerator()
            {
                return _references.GetEnumerator();
            }

            public Reference Item(object index) => throw new NotImplementedException();

            public Reference Find(string bstrIdentity) => throw new NotImplementedException();

            public Reference Add(string bstrPath) => throw new NotImplementedException();

            public Reference AddActiveX(string bstrTypeLibGuid, int lMajorVer = 0, int lMinorVer = 0, int lLocaleId = 0, string bstrWrapperTool = "") => throw new NotImplementedException();

            public Reference AddProject(Project pProject) => throw new NotImplementedException();

            public DTE DTE => throw new NotImplementedException();

            public object Parent => throw new NotImplementedException();

            public Project ContainingProject => throw new NotImplementedException();

            public int Count => throw new NotImplementedException();
        }
    }
}
