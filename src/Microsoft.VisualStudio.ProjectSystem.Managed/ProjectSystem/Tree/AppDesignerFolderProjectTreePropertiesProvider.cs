// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using ManagedPriorityOrder = Microsoft.VisualStudio.ProjectSystem.Order;

namespace Microsoft.VisualStudio.ProjectSystem.Tree
{
    /// <summary>
    ///     Provides a <see cref="IProjectTreePropertiesProvider"/> that handles the AppDesigner
    ///     folder, called "Properties" in C# and "My Project" in Visual Basic.
    /// </summary>
    [Export(typeof(IProjectTreePropertiesProvider))]
    [Export(typeof(IProjectTreeSettingsProvider))]
    [AppliesTo(ProjectCapability.AppDesigner)]
    [Order(ManagedPriorityOrder.Default)]
    internal class AppDesignerFolderProjectTreePropertiesProvider : AbstractSpecialFolderProjectTreePropertiesProvider, IProjectTreeSettingsProvider
    {
        private static readonly ProjectTreeFlags s_defaultFolderFlags = ProjectTreeFlags.Create(ProjectTreeFlags.Common.AppDesignerFolder | ProjectTreeFlags.Common.BubbleUp);

        private readonly IProjectDesignerService? _designerService;

        [ImportingConstructor]
        public AppDesignerFolderProjectTreePropertiesProvider(
            [Import(typeof(ProjectImageProviderAggregator))]IProjectImageProvider imageProvider,
            [Import(AllowDefault = true)] IProjectDesignerService? designerService)
            : base(imageProvider)
        {
            _designerService = designerService;
        }

        public override bool IsSupported
        {
            get { return _designerService?.SupportsProjectDesigner != false; }
        }

        public override ProjectTreeFlags FolderFlags
        {
            get { return s_defaultFolderFlags; }
        }

        public override string FolderImageKey
        {
            get { return ProjectImageKey.AppDesignerFolder; }
        }

        public override string ExpandedFolderImageKey
        {
            get { return ProjectImageKey.ExpandedAppDesignerFolder; }
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
            // See: https://github.com/dotnet/project-system/issues/209
            string folderName = ruleSnapshots.GetPropertyOrDefault(AppDesigner.SchemaName, AppDesigner.FolderNameProperty, "Properties");
            string contextsVisibleOnlyInShowAllFiles = ruleSnapshots.GetPropertyOrDefault(AppDesigner.SchemaName, AppDesigner.ContentsVisibleOnlyInShowAllFilesProperty, "false");

            projectTreeSettings = projectTreeSettings.SetItem($"{AppDesigner.SchemaName}.{AppDesigner.FolderNameProperty}", folderName);
            projectTreeSettings = projectTreeSettings.SetItem($"{AppDesigner.SchemaName}.{AppDesigner.ContentsVisibleOnlyInShowAllFilesProperty}", contextsVisibleOnlyInShowAllFiles);
        }

        protected sealed override bool IsCandidateSpecialFolder(IProjectTreeCustomizablePropertyContext propertyContext, ProjectTreeFlags flags)
        {
            if (propertyContext.ParentNodeFlags.IsProjectRoot() && flags.IsFolder() && flags.IsIncludedInProject())
            {
                string folderName = propertyContext.ProjectTreeSettings[$"{AppDesigner.SchemaName}.{AppDesigner.FolderNameProperty}"];

                return StringComparers.Paths.Equals(folderName, propertyContext.ItemName);
            }

            return false;
        }

        protected override bool AreContentsVisibleOnlyInShowAllFiles(IImmutableDictionary<string, string> projectTreeSettings)
        {
            return StringComparers.PropertyLiteralValues.Equals(projectTreeSettings[$"{AppDesigner.SchemaName}.{AppDesigner.ContentsVisibleOnlyInShowAllFilesProperty}"], "true");
        }
    }
}
