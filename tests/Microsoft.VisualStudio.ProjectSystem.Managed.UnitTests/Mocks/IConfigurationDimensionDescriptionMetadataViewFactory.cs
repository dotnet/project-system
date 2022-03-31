// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    internal static class IConfigurationDimensionDescriptionMetadataViewFactory
    {
        public static IConfigurationDimensionDescriptionMetadataView Create(string[] dimensionNames, bool[] isVariantDimension)
        {
            var mock = new Mock<IConfigurationDimensionDescriptionMetadataView>();
            mock.SetupGet(v => v.DimensionName)
                .Returns(dimensionNames);

            mock.SetupGet(v => v.IsVariantDimension)
                .Returns(isVariantDimension);

            return mock.Object;
        }
    }
}
