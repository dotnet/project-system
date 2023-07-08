// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    internal static class IImplicitlyActiveDimensionProviderFactory
    {
        public static IImplicitlyActiveDimensionProvider Create()
        {
            return Mock.Of<IImplicitlyActiveDimensionProvider>();
        }

        public static IImplicitlyActiveDimensionProvider ImplementGetImplicitlyActiveDimensions(Func<IEnumerable<string>, IEnumerable<string>> action)
        {
            var mock = new Mock<IImplicitlyActiveDimensionProvider>();
            mock.Setup(p => p.GetImplicitlyActiveDimensions(It.IsAny<IEnumerable<string>>()))
                .Returns(action);

            return mock.Object;
        }
    }
}
