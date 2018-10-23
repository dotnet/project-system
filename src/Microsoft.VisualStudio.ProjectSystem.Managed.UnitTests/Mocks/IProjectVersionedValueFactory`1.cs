// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectVersionedValueFactory<T>
    {
        internal static IProjectVersionedValue<T> Create(T value)
        {
            var mock = new Mock<IProjectVersionedValue<T>>();

            mock.SetupGet(p => p.Value)
                .Returns(value);

            mock.SetupGet(p => p.DataSourceVersions)
                .Returns(ImmutableDictionary<NamedIdentity, IComparable>.Empty);

            return mock.Object;
        }

        internal static IProjectVersionedValue<T> Create(T value, NamedIdentity identity, IComparable version)
        {
            var mock = new Mock<IProjectVersionedValue<T>>();

            mock.SetupGet(p => p.Value)
                .Returns(value);

            var dataSourceVersions = ImmutableDictionary<NamedIdentity, IComparable>.Empty.Add(identity, version);

            mock.SetupGet(p => p.DataSourceVersions)
                .Returns(dataSourceVersions);

            return mock.Object;
        }
    }
}
