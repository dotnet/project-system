// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides a <see cref="IProjectTreePropertiesProvider"/> that handles the AppDesigner 
    ///     folder, called "Properties" in C# and "My Project" in Visual Basic.
    /// </summary>
    [Export(typeof(IProjectTreePropertiesProvider))]
    [Export(typeof(IProjectTreeSettingsProvider))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class AppDesignerFolderProjectTreePropertiesProvider : AbstractSpecialFolderProjectTreePropertiesProvider, IProjectTreeSettingsProvider
    {
        private static readonly ProjectTreeFlags DefaultFolderFlags = ProjectTreeFlags.Create(ProjectTreeFlags.Common.AppDesignerFolder | ProjectTreeFlags.Common.BubbleUp);

        private readonly IProjectDesignerService _designerService;

        [ImportingConstructor]
        public AppDesignerFolderProjectTreePropertiesProvider([Import(typeof(ProjectImageProviderAggregator))]IProjectImageProvider imageProvider, IProjectDesignerService designerService)
            : base(imageProvider)
        {
            Requires.NotNull(designerService, nameof(designerService));

            _designerService = designerService;
        }

        public override bool IsSupported
        {
            get { return _designerService.SupportsProjectDesigner; }
        }

        public override ProjectTreeFlags FolderFlags
        {
            get { return DefaultFolderFlags; }
        }

        public override string FolderImageKey
        {
            get {  return ProjectImageKey.AppDesignerFolder; }
        }

        public ICollection<string> ProjectPropertiesRules
        {
            get { return AppDesigner.SchemaNameArray; }
        }

        public void UpdateProjectTreeSettings(IImmutableDictionary<string, IProjectRuleSnapshot> ruleSnapshots, ref IImmutableDictionary<string, string> projectTreeSettings)
        {
            Requires.NotNull(ruleSnapshots, nameof(ruleSnapshots));
            Requires.NotNull(projectTreeSettings, nameof(projectTreeSettings));

            // Retrieves the <AppDesignerFolder> and <AppDesignerFolderContentsVisibleOnlyInShowAllFiles> properties from the project file
            //
            // TODO: Read these default values from the rules themselves
            // See: https://github.com/dotnet/roslyn-project-system/issues/209
            string folderName = ruleSnapshots.GetPropertyOrDefault(AppDesigner.SchemaName, AppDesigner.FolderNameProperty, "Properties");
            string contextsVisibleOnlyInShowAllFiles = ruleSnapshots.GetPropertyOrDefault(AppDesigner.SchemaName, AppDesigner.ContentsVisibleOnlyInShowAllFilesProperty, "false");

            projectTreeSettings = projectTreeSettings.SetItem(AppDesigner.FolderNameProperty, folderName);
            projectTreeSettings = projectTreeSettings.SetItem(AppDesigner.ContentsVisibleOnlyInShowAllFilesProperty, contextsVisibleOnlyInShowAllFiles);
        }

        protected override sealed bool IsCandidateSpecialFolder(IProjectTreeCustomizablePropertyContext propertyContext, ProjectTreeFlags flags)
        {
            if (propertyContext.ParentNodeFlags.IsProjectRoot() && flags.IsFolder() && flags.IsIncludedInProject())
            {
                string folderName = propertyContext.ProjectTreeSettings[AppDesigner.FolderNameProperty];

                return StringComparers.Paths.Equals(folderName, propertyContext.ItemName);
            }

            return false;
        }

        protected override bool AreContentsVisibleOnlyInShowAllFiles(IImmutableDictionary<string, string> projectTreeSettings)
        {
            return StringComparers.PropertyValues.Equals(projectTreeSettings[AppDesigner.ContentsVisibleOnlyInShowAllFilesProperty], "true");
        }
    }
}
