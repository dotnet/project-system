// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectVersionedValueFactory
    {
        public static IProjectVersionedValue<IProjectSubscriptionUpdate> CreateEmpty()
        {
            return FromJson("{}");
        }

        internal static IProjectVersionedValue<T> Create<T>(T value)
        {
            var mock = new Mock<IProjectVersionedValue<T>>();

            mock.SetupGet(p => p.Value)
                .Returns(value);

            mock.SetupGet(p => p.DataSourceVersions)
                .Returns(ImmutableDictionary<NamedIdentity, IComparable>.Empty);

            return mock.Object;
        }

        internal static IProjectVersionedValue<T> Create<T>(T value, IImmutableDictionary<NamedIdentity, IComparable> dataSourceVersions)
        {
            var mock = new Mock<IProjectVersionedValue<T>>();

            mock.SetupGet(p => p.Value)
                .Returns(value);

            mock.SetupGet(p => p.DataSourceVersions)
                .Returns(dataSourceVersions);

            return mock.Object;
        }

        internal static IProjectVersionedValue<T> Create<T>(T value, NamedIdentity identity, IComparable version)
        {
            var mock = new Mock<IProjectVersionedValue<T>>();

            mock.SetupGet(p => p.Value)
                .Returns(value);

            var dataSourceVersions = ImmutableDictionary<NamedIdentity, IComparable>.Empty.Add(identity, version);

            mock.SetupGet(p => p.DataSourceVersions)
                .Returns(dataSourceVersions);

            return mock.Object;
        }

        public static IProjectVersionedValue<IProjectSubscriptionUpdate> FromJson(string jsonString)
        {
            return FromJson(version: 1, jsonString);
        }

        public static IProjectVersionedValue<IProjectSubscriptionUpdate> FromJson(IComparable version, string jsonString)
        {
            var update = IProjectSubscriptionUpdateFactory.FromJson(jsonString);

            // Every IProjectSubscriptionUpdate contains the version of the configured project
            return Create(update, identity: ProjectDataSources.ConfiguredProjectVersion, version);
        }
    }
}
