// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IProjectCapabilitiesScopeFactory
    {
        public static IProjectCapabilitiesScope Create(IEnumerable<string>? capabilities = null)
        {
            capabilities ??= Enumerable.Empty<string>();
            var snapshot = new Mock<IProjectCapabilitiesSnapshot>();
            snapshot.Setup(s => s.IsProjectCapabilityPresent(It.IsAny<string>())).Returns((string capability) => capabilities.Contains(capability));

            var versionedValue = new Mock<IProjectVersionedValue<IProjectCapabilitiesSnapshot>>();
            versionedValue.Setup(v => v.Value).Returns(snapshot.Object);

            var scope = new Mock<IProjectCapabilitiesScope>();
            scope.Setup(s => s.Current).Returns(versionedValue.Object);

            return scope.Object;
        }
    }
}
