// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     A project tree properties provider that turns "My Project" folder into a special folder.
    /// </summary>
    [Export(typeof(IProjectTreePropertiesProvider))]
    [AppliesTo(ProjectCapability.VisualBasic)]
    internal class MyProjectFolderProjectTreePropertiesProvider : AbstractAppDesignerFolderProjectTreePropertiesProvider
    {
        [ImportingConstructor]
        public MyProjectFolderProjectTreePropertiesProvider([Import(typeof(ProjectImageProviderAggregator))]IProjectImageProvider imageProvider, IUnconfiguredProjectCommonServices projectServices, IProjectDesignerService designerService)
            : base(imageProvider, projectServices, designerService)
        {
        }

        public override bool IsExpandableByDefault
        {
            get { return false; }   // By default, My Project isn't expandable unless Show All Files is on
        }

        protected override string GetAppDesignerFolderName()
        {
            string folderName = base.GetAppDesignerFolderName();
            if (!string.IsNullOrEmpty(folderName))
                return folderName;

            return "My Project";        // Not localized
        }
    }
}
