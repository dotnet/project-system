// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using VSLangProj;

namespace VSLangProj80
{
    internal static class Reference3Factory
    {
        public static Reference3 CreateComReference()
        {
            var mock = new Mock<Reference3>();
            mock.SetupGet(r => r.Type)
                .Returns(prjReferenceType.prjReferenceTypeActiveX);

            return mock.Object;
        }

        public static Reference3 CreateAssemblyReference(
            string name,
            string? version = null,
            string? path = null,
            prjReferenceType type = prjReferenceType.prjReferenceTypeAssembly,
            __PROJECTREFERENCETYPE refType = __PROJECTREFERENCETYPE.PROJREFTYPE_ASSEMBLY)
        {
            var mock = new Mock<Reference3>();
            mock.SetupGet(r => r.Name)
                .Returns(name);

            mock.SetupGet<string?>(r => r.Version)
                .Returns(version);

            mock.SetupGet<string?>(r => r.Path)
                .Returns(path);

            mock.SetupGet(r => r.Resolved)
                .Returns(path is not null);

            mock.SetupGet(r => r.Type)
                .Returns(type);

            mock.SetupGet(r => r.RefType)
              .Returns((uint)refType);

            return mock.Object;
        }
    }
}
