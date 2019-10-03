// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Moq;

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
