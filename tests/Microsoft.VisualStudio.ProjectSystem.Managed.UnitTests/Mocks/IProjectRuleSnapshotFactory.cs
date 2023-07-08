// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    internal static class IProjectRuleSnapshotFactory
    {
        public static IProjectRuleSnapshot Create(string ruleName, string propertyName, string propertyValue, IImmutableDictionary<string, IImmutableDictionary<string, string>>? items = null)
        {
            var mock = new Mock<IProjectRuleSnapshot>();
            mock.SetupGet(r => r.RuleName)
                .Returns(ruleName);

            var dictionary = ImmutableStringDictionary<string>.EmptyOrdinal.Add(propertyName, propertyValue);

            mock.SetupGet(r => r.Properties)
                .Returns(dictionary);

            if (items is not null)
            {
                mock.SetupGet(c => c.Items).Returns(items);
            }

            return mock.Object;
        }

        public static IProjectRuleSnapshot Add(this IProjectRuleSnapshot snapshot, string propertyName, string propertyValue)
        {
            var mock = Mock.Get(snapshot);

            var dictionary = snapshot.Properties.Add(propertyName, propertyValue);

            mock.SetupGet(r => r.Properties)
                .Returns(dictionary);

            return mock.Object;
        }

        public static IProjectRuleSnapshot FromJson(string jsonString)
        {
            var model = new IProjectRuleSnapshotModel();
            return model.FromJson(jsonString);
        }
    }

    internal class IProjectRuleSnapshotModel : JsonModel<IProjectRuleSnapshot>
    {
        public Dictionary<string, IImmutableDictionary<string, string>> Items { get; set; } = new();
        public Dictionary<string, string> Properties { get; set; } = new();
        public string RuleName { get; set; } = "";
        public bool EvaluationSucceeded { get; set; } = true;

        public override IProjectRuleSnapshot ToActualModel()
        {
            return new ActualModel(
                new ImmutableOrderedDictionary<string, IImmutableDictionary<string, string>>(Items),
                new ImmutableOrderedDictionary<string, string>(Properties),
                RuleName,
                EvaluationSucceeded);
        }

        private sealed class ActualModel : IProjectRuleSnapshot, IProjectRuleSnapshotEvaluationStatus
        {
            public IImmutableDictionary<string, IImmutableDictionary<string, string>> Items { get; }
            public IImmutableDictionary<string, string> Properties { get; }
            public string RuleName { get; }
            public bool EvaluationSucceeded { get; }

            public ActualModel(IImmutableDictionary<string, IImmutableDictionary<string, string>> items, IImmutableDictionary<string, string> properties, string ruleName, bool evaluationSucceeded)
            {
                Items = items;
                Properties = properties;
                RuleName = ruleName;
                EvaluationSucceeded = evaluationSucceeded;
            }
        }
    }
}
