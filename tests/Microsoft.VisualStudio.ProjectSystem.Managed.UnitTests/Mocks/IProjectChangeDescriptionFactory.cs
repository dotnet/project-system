// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectChangeDescriptionFactory
    {
        public static IProjectChangeDescription Create()
        {
            return Mock.Of<IProjectChangeDescription>();
        }

        public static IProjectChangeDescription FromJson(string jsonString)
        {
            var model = new IProjectChangeDescriptionModel();
            return model.FromJson(jsonString);
        }
    }

    internal class IProjectChangeDescriptionModel : JsonModel<IProjectChangeDescription>
    {
        public IProjectRuleSnapshotModel After { get; set; } = new IProjectRuleSnapshotModel();
        public IProjectRuleSnapshotModel Before { get; set; } = new IProjectRuleSnapshotModel();
        public IProjectChangeDiffModel Difference { get; set; } = new IProjectChangeDiffModel();

        public override IProjectChangeDescription ToActualModel()
        {
            return new ActualModel(After.ToActualModel(), Before.ToActualModel(), Difference);
        }

        private sealed class ActualModel : IProjectChangeDescription
        {
            public IProjectRuleSnapshot After { get; }
            public IProjectRuleSnapshot Before { get; }
            public IProjectChangeDiff Difference { get; }

            public ActualModel(IProjectRuleSnapshot after, IProjectRuleSnapshot before, IProjectChangeDiff difference)
            {
                After = after;
                Before = before;
                Difference = difference;
            }
        }
    }
}
