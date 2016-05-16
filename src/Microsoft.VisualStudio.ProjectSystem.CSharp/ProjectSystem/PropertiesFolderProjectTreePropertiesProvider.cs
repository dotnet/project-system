// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     A project tree properties provider that turns "Properties" folder into a special folder.
    /// </summary>
    [Export(typeof(IProjectTreePropertiesProvider))]
    [AppliesTo(ProjectCapability.CSharp)]
    internal class PropertiesFolderProjectTreePropertiesProvider : AbstractAppDesignerFolderProjectTreePropertiesProvider
    {
        [ImportingConstructor]
        public PropertiesFolderProjectTreePropertiesProvider([Import(typeof(ProjectImageProviderAggregator))]IProjectImageProvider imageProvider, IUnconfiguredProjectCommonServices projectServices, IProjectDesignerService designerService)
            : base(imageProvider, projectServices, designerService)
        {
        }
        
        public override bool ContentsVisibleOnlyInShowAllFiles
        {
            get { return false; }
        }

        protected override string GetAppDesignerFolderName()
        {
            string folderName = base.GetAppDesignerFolderName();
            if (!string.IsNullOrEmpty(folderName))
                return folderName;

            return "Properties";        // Not localized
        }
    }
}
