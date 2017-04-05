// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using Moq;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    internal static class IProjectRuleSnapshotFactory
    {
        public static IProjectRuleSnapshot Create(string ruleName)
        {
            var mock = new Mock<IProjectRuleSnapshot>();
            mock.SetupGet(r => r.RuleName)
                .Returns(ruleName);

            var properties = ImmutableDictionary<string, string>.Empty;

            mock.SetupGet(r => r.Properties)
                .Returns(properties);

            var items = ImmutableDictionary<string, IImmutableDictionary<string, string>>.Empty;

            mock.SetupGet(r => r.Items)
                .Returns(items);

            return mock.Object;
        }

        public static IProjectRuleSnapshot Create(string ruleName, string propertyName, string propertyValue)
        {
            var mock = new Mock<IProjectRuleSnapshot>();
            mock.SetupGet(r => r.RuleName)
                .Returns(ruleName);

            var properties = ImmutableDictionary<string, string>.Empty.Add(propertyName, propertyValue);

            mock.SetupGet(r => r.Properties)
                .Returns(properties);

            var items = ImmutableDictionary<string, IImmutableDictionary<string, string>>.Empty;

            mock.SetupGet(r => r.Items)
                .Returns(items);

            return mock.Object;
        }

        public static IProjectRuleSnapshot AddProperty(this IProjectRuleSnapshot snapshot, string propertyName, string propertyValue)
        {
            var mock = Mock.Get(snapshot);

            var dictionary = snapshot.Properties.Add(propertyName, propertyValue);

            mock.SetupGet(r => r.Properties)
                .Returns(dictionary);

            return mock.Object;
        }

        public static IProjectRuleSnapshot AddItem(this IProjectRuleSnapshot snapshot, string item, IImmutableDictionary<string, string> itemProperties = null)
        {
            var mock = Mock.Get(snapshot);

            if (itemProperties == null)
            {
                itemProperties = ImmutableDictionary<string, string>.Empty;
            }

            var dictionary = snapshot.Items.Add(item, itemProperties);

            mock.SetupGet(r => r.Items)
                .Returns(dictionary);

            return mock.Object;
        }
    }
}
