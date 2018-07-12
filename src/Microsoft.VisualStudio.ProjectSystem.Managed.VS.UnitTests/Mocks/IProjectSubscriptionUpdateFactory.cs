// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.VisualStudio.ProjectSystem.Properties;

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IProjectSubscriptionUpdateFactory
    {
        public static IProjectVersionedValue<IProjectSubscriptionUpdate> CreateEmptyVersionedValue()
        {
            return IProjectVersionedValueFactory<IProjectSubscriptionUpdate>.Create(Mock.Of<IProjectSubscriptionUpdate>());
        }

        public static IProjectSubscriptionUpdate Create()
        {
            return Mock.Of<IProjectSubscriptionUpdate>();
        }

        public static IProjectSubscriptionUpdate Implement(
                        IDictionary<string, IProjectRuleSnapshot> currentState = null,
                        IDictionary<string, IProjectChangeDescription> projectChanges = null,
                        MockBehavior? mockBehavior = null)
        {
            var behavior = mockBehavior ?? MockBehavior.Default;
            var mock = new Mock<IProjectSubscriptionUpdate>(behavior);

            if (currentState != null)
            {
                mock.Setup(x => x.CurrentState).Returns(currentState.ToImmutableDictionary());
            }

            if (projectChanges != null)
            {
                mock.Setup(x => x.ProjectChanges).Returns(projectChanges.ToImmutableDictionary());
            }

            return mock.Object;
        }

        public static IProjectSubscriptionUpdate FromJson(string jsonString)
        {
            var model = new IProjectSubscriptionUpdateModel();
            return model.FromJson(jsonString);
        }
    }

    internal class IProjectSubscriptionUpdateModel : JsonModel<IProjectSubscriptionUpdate>
    {
        public IImmutableDictionary<string, IProjectRuleSnapshotModel> CurrentState { get; set; }
        public IImmutableDictionary<string, IProjectChangeDescriptionModel> ProjectChanges { get; set; }
        public ProjectConfigurationModel ProjectConfiguration { get; set; }

        public override IProjectSubscriptionUpdate ToActualModel()
        {
            var model = new ActualModel();

            if (CurrentState != null)
            {
                model.CurrentState = CurrentState.Select(x => new KeyValuePair<string, IProjectRuleSnapshot>(x.Key, x.Value))
                                        .ToImmutableDictionary();
            }

            if (ProjectChanges != null)
            {
                model.ProjectChanges = ProjectChanges.Select(x => new KeyValuePair<string, IProjectChangeDescription>(x.Key, x.Value.ToActualModel()))
                                            .ToImmutableDictionary();
            }

            if (ProjectConfiguration != null)
            {
                model.ProjectConfiguration = ProjectConfiguration.ToActualModel();
            }

            return model;
        }

        private class ActualModel : IProjectSubscriptionUpdate
        {
            public IImmutableDictionary<string, IProjectRuleSnapshot> CurrentState { get; set; }
            public IImmutableDictionary<string, IProjectChangeDescription> ProjectChanges { get; set; }
            public ProjectConfiguration ProjectConfiguration { get; set; }
        }
    }
}
