// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Responsible for producing AdditionalDesignTimeBuildInput items (such as project.assets.json)
    ///     so that they can be watched, and when changed, trigger re-evaluation.
    /// </summary>
    [Export(typeof(IFileWatchDataSource))]
    [AppliesTo(ProjectCapability.PackageReferences)]
    internal class AdditionalDesignTimeBuildInputDataSource : AbstractItemFileWatchDataSource
    {
        [ImportingConstructor]
        public AdditionalDesignTimeBuildInputDataSource(ConfiguredProject project, IProjectSubscriptionService projectSubscriptionService)
            : base(project, projectSubscriptionService)
        {
        }

        public override string ItemSchemaName => AdditionalDesignTimeBuildInput.SchemaName;

        public override string FullPathProperty => AdditionalDesignTimeBuildInput.FullPathProperty;

        public override FileWatchChangeKinds FileWatchChangeKinds => FileWatchChangeKinds.Added | FileWatchChangeKinds.Removed | FileWatchChangeKinds.Changed;

    }
}
