// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;
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
                .Returns(path != null);

            mock.SetupGet(r => r.Type)
                .Returns(type);

            mock.SetupGet(r => r.RefType)
              .Returns((uint)refType);

            return mock.Object;
        }
    }
}
