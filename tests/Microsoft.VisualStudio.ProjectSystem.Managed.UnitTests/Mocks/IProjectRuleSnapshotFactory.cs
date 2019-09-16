// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    internal static class IProjectRuleSnapshotFactory
    {
        public static IProjectRuleSnapshot Create(string ruleName, string propertyName, string propertyValue)
        {
            var mock = new Mock<IProjectRuleSnapshot>();
            mock.SetupGet(r => r.RuleName)
                .Returns(ruleName);

            var dictionary = ImmutableStringDictionary<string>.EmptyOrdinal.Add(propertyName, propertyValue);

            mock.SetupGet(r => r.Properties)
                .Returns(dictionary);


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

    internal class IProjectRuleSnapshotModel : JsonModel<IProjectRuleSnapshot>, IProjectRuleSnapshot, IProjectRuleSnapshotEvaluationStatus
    {
        public IImmutableDictionary<string, IImmutableDictionary<string, string>> Items { get; set; } = ImmutableDictionary<string, IImmutableDictionary<string, string>>.Empty;
        public IImmutableDictionary<string, string> Properties { get; set; } = ImmutableDictionary<string, string>.Empty;
        public string RuleName { get; set; } = "";
        public bool EvaluationSucceeded { get; set; } = true;

        public override IProjectRuleSnapshot ToActualModel()
        {
            return this;
        }
    }
}
