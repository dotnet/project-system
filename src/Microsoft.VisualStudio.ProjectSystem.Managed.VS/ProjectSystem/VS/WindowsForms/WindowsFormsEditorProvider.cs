// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.WindowsForms
{
    /// <summary>
    ///     A project-specific editor provider that is responsible for handling two things;
    ///
    ///     1) Add the Windows Forms designer to the list of editor factories for a "designable" source file, and
    ///        determines whether it opens by default.
    ///
    ///     2) Persists whether the designer opens by default when the user uses Open With -> Set As Default.
    /// </summary>
    [Export(typeof(IProjectSpecificEditorProvider))]
    [AppliesTo(ProjectCapability.DotNet)]
    [Order(Order.BeforeDefault)] // Need to run before CPS's version before its deleted
    internal partial class WindowsFormsEditorProvider : IProjectSpecificEditorProvider
    {
        private static readonly SubTypeDescriptor[] s_subTypeDescriptors = new[]
        {
            new SubTypeDescriptor("Form",           VSResources.WindowsFormEditor_DisplayName, useDesignerByDefault: true),
            new SubTypeDescriptor("Designer",       VSResources.WindowsFormEditor_DisplayName, useDesignerByDefault: true),
            new SubTypeDescriptor("UserControl",    VSResources.UserControlEditor_DisplayName, useDesignerByDefault: true),
            new SubTypeDescriptor("Component",      VSResources.ComponentEditor_DisplayName,   useDesignerByDefault: false)
        };

        private readonly UnconfiguredProject _project;
        private readonly Lazy<IPhysicalProjectTree> _projectTree;
        private readonly Lazy<IProjectSystemOptions> _options;

        [ImportingConstructor]
        public WindowsFormsEditorProvider(UnconfiguredProject project, Lazy<IPhysicalProjectTree> projectTree, Lazy<IProjectSystemOptions> options)
        {
            _project = project;
            _projectTree = projectTree;
            _options = options;

            ProjectSpecificEditorProviders = new OrderPrecedenceImportCollection<IProjectSpecificEditorProvider, INamedExportMetadataView>(projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        public OrderPrecedenceImportCollection<IProjectSpecificEditorProvider, INamedExportMetadataView> ProjectSpecificEditorProviders { get; }

        public async Task<IProjectSpecificEditorInfo?> GetSpecificEditorAsync(string documentMoniker)
        {
            Requires.NotNullOrEmpty(documentMoniker, nameof(documentMoniker));

            IProjectSpecificEditorInfo? editor = await GetDefaultEditorAsync(documentMoniker);
            if (editor is null)
                return null;

            SubTypeDescriptor? descriptor = await GetSubTypeDescriptorAsync(documentMoniker);
            if (descriptor is null)
                return null;

            bool isDefaultEditor = await _options.Value.GetUseDesignerByDefaultAsync(descriptor.SubType, descriptor.UseDesignerByDefault);

            return new EditorInfo(editor.EditorFactory, descriptor.DisplayName, isDefaultEditor);
        }

        public async Task<bool> SetUseGlobalEditorAsync(string documentMoniker, bool useGlobalEditor)
        {
            Requires.NotNullOrEmpty(documentMoniker, nameof(documentMoniker));

            SubTypeDescriptor? editorInfo = await GetSubTypeDescriptorAsync(documentMoniker);
            if (editorInfo is null)
                return false;

            // 'useGlobalEditor' means use the default editor that is registered for source files
            await _options.Value.SetUseDesignerByDefaultAsync(editorInfo.SubType, !useGlobalEditor);
            return true;
        }

        private async Task<IProjectSpecificEditorInfo?> GetDefaultEditorAsync(string documentMoniker)
        {
            IProjectSpecificEditorProvider? defaultProvider = GetDefaultEditorProvider();
            if (defaultProvider is null)
                return null;

            return await defaultProvider.GetSpecificEditorAsync(documentMoniker);
        }

        private async Task<SubTypeDescriptor?> GetSubTypeDescriptorAsync(string documentMoniker)
        {
            string? subType = await GetSubTypeAsync(documentMoniker);
            if (subType is not null)
            {
                foreach (SubTypeDescriptor descriptor in s_subTypeDescriptors)
                {
                    if (StringComparers.PropertyLiteralValues.Equals(subType, descriptor.SubType))
                        return descriptor;
                }
            }

            return null;
        }

        private async Task<string?> GetSubTypeAsync(string documentMoniker)
        {
            IProjectItemTree? item = await FindCompileItemByMonikerAsync(documentMoniker);
            if (item is null)
                return null;

            ConfiguredProject? project = await _project.GetSuggestedConfiguredProjectAsync();

            IRule? browseObject = GetBrowseObjectProperties(project!, item);
            if (browseObject is null)
                return null;

            return await browseObject.GetPropertyValueAsync(Compile.SubTypeProperty);
        }

        protected virtual IRule? GetBrowseObjectProperties(ConfiguredProject project, IProjectItemTree item)
        {
            // For unit testing purposes
            return item.GetBrowseObjectPropertiesViaSnapshotIfAvailable(project);
        }

        private async Task<IProjectItemTree?> FindCompileItemByMonikerAsync(string documentMoniker)
        {
            IProjectTreeServiceState result = await _projectTree.Value.TreeService.PublishAnyNonLoadingTreeAsync();

            if (result.TreeProvider.FindByPath(result.Tree, documentMoniker) is IProjectItemTree treeItem &&
                treeItem.Parent?.Flags.Contains(ProjectTreeFlags.SourceFile) == false &&
                StringComparers.ItemTypes.Equals(treeItem.Item?.ItemType, Compile.SchemaName))
            {
                return treeItem;
            }

            return null;
        }

        private IProjectSpecificEditorProvider? GetDefaultEditorProvider()
        {
            Lazy<IProjectSpecificEditorProvider>? editorProvider = ProjectSpecificEditorProviders.FirstOrDefault(p => string.Equals(p.Metadata.Name, "Default", StringComparisons.NamedExports));

            return editorProvider?.Value;
        }
    }
}
