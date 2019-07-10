// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [Export(typeof(IFileWatchDataSource))]
    [AppliesTo(ProjectCapability.DotNetLanguageService)]
    internal class PotentialEditorConfigFilesDataSource : AbstractItemFileWatchDataSource
    {
        [ImportingConstructor]
        public PotentialEditorConfigFilesDataSource(ConfiguredProject project, IProjectSubscriptionService projectSubscriptionService)
           : base(project, projectSubscriptionService)
        {
        }

        public override string ItemSchemaName => PotentialEditorConfigFiles.SchemaName;

        public override string FullPathProperty => PotentialEditorConfigFiles.FullPathProperty;

        public override FileWatchChangeKinds FileWatchChangeKinds => FileWatchChangeKinds.Added | FileWatchChangeKinds.Removed;
    }
}
