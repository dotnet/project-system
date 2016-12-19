// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IProjectRuleSnapshotFactory
    {
        public static IProjectRuleSnapshot Create()
        {
            return Mock.Of<IProjectRuleSnapshot>();
        }

        public static IProjectRuleSnapshot Implement(
                            string ruleName = null,
                            IDictionary<string, IImmutableDictionary<string, string>> items = null,
                            IDictionary<string, string> properties = null,
                            MockBehavior? mockBehavior = null)
        {
            var behavior = mockBehavior ?? MockBehavior.Default;
            var mock = new Mock<IProjectRuleSnapshot>(behavior);

            if (ruleName != null)
            {
                mock.Setup(x => x.RuleName).Returns(ruleName);
            }

            if (items != null)
            {
                mock.Setup(x => x.Items).Returns(items.ToImmutableDictionary());
            }

            if (properties != null)
            {
                mock.Setup(x => x.Properties).Returns(properties.ToImmutableDictionary());
            }

            return mock.Object;
        }

        public static IProjectRuleSnapshot FromJson(string jsonString)
        {
            var model = new IProjectRuleSnapshotModel();
            return model.FromJson(jsonString);
        }
    }

    internal class IProjectRuleSnapshotModel : JsonModel<IProjectRuleSnapshot>, IProjectRuleSnapshot
    {
        public IImmutableDictionary<string, IImmutableDictionary<string, string>> Items { get; set; }
        public IImmutableDictionary<string, string> Properties { get; set; }
        public string RuleName { get; set; }

        public IImmutableDictionary<NamedIdentity, IComparable> DataSourceVersions { get; }

        public override IProjectRuleSnapshot ToActualModel()
        {
            return this;
        }
    }
}