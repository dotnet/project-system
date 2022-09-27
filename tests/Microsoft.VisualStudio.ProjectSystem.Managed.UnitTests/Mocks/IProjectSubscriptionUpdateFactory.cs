// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectSubscriptionUpdateFactory
    {
        public static IProjectVersionedValue<IProjectSubscriptionUpdate> CreateEmptyVersionedValue()
        {
            return IProjectVersionedValueFactory.Create(Mock.Of<IProjectSubscriptionUpdate>());
        }

        public static IProjectSubscriptionUpdate Implement(
            IDictionary<string, IProjectRuleSnapshot>? currentState = null,
            IDictionary<string, IProjectChangeDescription>? projectChanges = null,
            MockBehavior mockBehavior = MockBehavior.Default)
        {
            var mock = new Mock<IProjectSubscriptionUpdate>(mockBehavior);

            mock.Setup(x => x.CurrentState).Returns(currentState?.ToImmutableDictionary() ?? ImmutableDictionary<string, IProjectRuleSnapshot>.Empty);

            mock.Setup(x => x.ProjectChanges).Returns(projectChanges?.ToImmutableDictionary() ?? ImmutableDictionary<string, IProjectChangeDescription>.Empty);

            return mock.Object;
        }

        public static IProjectSubscriptionUpdate CreateEmpty()
        {
            return FromJson(
                """
                {
                    "CurrentState": {
                    },
                    "ProjectChanges": {
                    }
                }
                """);
        }

        public static IProjectSubscriptionUpdate FromJson(string jsonString)
        {
            var model = new IProjectSubscriptionUpdateModel();
            return model.FromJson(jsonString);
        }
    }

    internal class IProjectSubscriptionUpdateModel : JsonModel<IProjectSubscriptionUpdate>
    {
        public IImmutableDictionary<string, IProjectRuleSnapshotModel>? CurrentState { get; set; }
        public IImmutableDictionary<string, IProjectChangeDescriptionModel>? ProjectChanges { get; set; }
        public ProjectConfigurationModel? ProjectConfiguration { get; set; }

        public override IProjectSubscriptionUpdate ToActualModel()
        {
            IImmutableDictionary<string, IProjectRuleSnapshot> currentState;
            IImmutableDictionary<string, IProjectChangeDescription> projectChanges;
            ProjectConfiguration projectConfiguration;

            if (CurrentState is not null)
            {
                currentState = CurrentState.Select(x => new KeyValuePair<string, IProjectRuleSnapshot>(x.Key, x.Value.ToActualModel())).ToImmutableDictionary();
            }
            else
            {
                currentState = ImmutableDictionary<string, IProjectRuleSnapshot>.Empty;
            }

            if (ProjectChanges is not null)
            {
                projectChanges = ProjectChanges.Select(x => new KeyValuePair<string, IProjectChangeDescription>(x.Key, x.Value.ToActualModel())).ToImmutableDictionary();
            }
            else
            {
                projectChanges = ImmutableDictionary<string, IProjectChangeDescription>.Empty;
            }

            if (ProjectConfiguration is not null)
            {
                projectConfiguration = ProjectConfiguration.ToActualModel();
            }
            else
            {
                projectConfiguration = ProjectConfigurationFactory.Create("TEST");
            }

            return new ActualModel(currentState, projectChanges, projectConfiguration);
        }

        private class ActualModel : IProjectSubscriptionUpdate
        {
            public IImmutableDictionary<string, IProjectRuleSnapshot> CurrentState { get; }
            public IImmutableDictionary<string, IProjectChangeDescription> ProjectChanges { get; }
            public ProjectConfiguration ProjectConfiguration { get; }

            public ActualModel(IImmutableDictionary<string, IProjectRuleSnapshot> currentState, IImmutableDictionary<string, IProjectChangeDescription> projectChanges, ProjectConfiguration projectConfiguration)
            {
                CurrentState = currentState;
                ProjectChanges = projectChanges;
                ProjectConfiguration = projectConfiguration;
            }
        }
    }
}
