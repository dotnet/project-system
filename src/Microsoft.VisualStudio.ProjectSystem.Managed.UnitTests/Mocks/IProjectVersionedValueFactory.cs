// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal class IProjectVersionedValueFactory<T>
    {
        internal static IProjectVersionedValue<T> Create(T value)
        {
            var mock = new Mock<IProjectVersionedValue<T>>();

            mock.SetupGet(p => p.Value).Returns(value);

            return mock.Object;
        }
    }
}
