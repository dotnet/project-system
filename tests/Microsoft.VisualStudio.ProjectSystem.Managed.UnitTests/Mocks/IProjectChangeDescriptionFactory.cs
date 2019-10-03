// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Moq;

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
            return new ActualModel(After, Before, Difference);
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
