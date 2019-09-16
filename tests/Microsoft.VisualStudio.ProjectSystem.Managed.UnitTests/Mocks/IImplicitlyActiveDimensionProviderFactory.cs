// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Moq;

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
