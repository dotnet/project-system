// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

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
