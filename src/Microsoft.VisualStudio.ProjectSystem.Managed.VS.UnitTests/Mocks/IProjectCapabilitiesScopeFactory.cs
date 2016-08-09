// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IProjectCapabilitiesScopeFactory
    {
        public static IProjectCapabilitiesScope Create()
        {
            var versionedValue = new Mock<IProjectVersionedValue<IProjectCapabilitiesSnapshot>>();
            versionedValue.Setup(v => v.Value).Returns(Mock.Of<IProjectCapabilitiesSnapshot>());

            var scope = new Mock<IProjectCapabilitiesScope>();
            scope.Setup(s => s.Current).Returns(versionedValue.Object);

            return scope.Object;
        }
    }
}
