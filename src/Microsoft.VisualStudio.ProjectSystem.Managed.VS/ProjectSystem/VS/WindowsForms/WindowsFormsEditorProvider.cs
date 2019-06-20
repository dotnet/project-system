// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

#nullable enable

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
    internal partial class WindowsFormEditorProvider : IProjectSpecificEditorProvider
    {
        private readonly static SubTypeDescriptor[] SubTypeDescriptors = new[]
        {
            new SubTypeDescriptor("Form",           VSResources.WindowsFormEditor_DisplayName, "Form",         useDesignerByDefault: true), // "Form" and "Designer" represent the same thing
            new SubTypeDescriptor("Designer",       VSResources.WindowsFormEditor_DisplayName, "Form",         useDesignerByDefault: true),
            new SubTypeDescriptor("UserControl",    VSResources.UserControlEditor_DisplayName, "UserControl",  useDesignerByDefault: true),
            new SubTypeDescriptor("Component",      VSResources.ComponentEditor_DisplayName,   "Component",    useDesignerByDefault: false)
        };

        private readonly Lazy<IPhysicalProjectTree> _projectTree;
        private readonly Lazy<IProjectSystemOptions> _options;

        [ImportingConstructor]
        public WindowsFormEditorProvider(UnconfiguredProject unconfiguredProject, Lazy<IPhysicalProjectTree> projectTree, Lazy<IProjectSystemOptions> options)
        {
            _projectTree = projectTree;
            _options = options;

            ProjectSpecificEditorProviders = new OrderPrecedenceImportCollection<IProjectSpecificEditorProvider, INamedExportMetadataView>(projectCapabilityCheckProvider: unconfiguredProject);
        }

        [ImportMany]
        public OrderPrecedenceImportCollection<IProjectSpecificEditorProvider, INamedExportMetadataView> ProjectSpecificEditorProviders
        {
            get;
        }

        public async Task<IProjectSpecificEditorInfo?> GetSpecificEditorAsync(string documentMoniker)
        {
            Requires.NotNullOrEmpty(documentMoniker, nameof(documentMoniker));

            IProjectSpecificEditorInfo? editor = await GetDefaultEditorAsync(documentMoniker);
            if (editor == null)
                return null;

            SubTypeDescriptor? descriptor = await GetSubTypeDescriptorAsync(documentMoniker);
            if (descriptor == null)
                return null;

            bool isDefaultEditor = await _options.Value.GetUseDesignerByDefaultAsync(descriptor.DesignerCategoryForPersistence, descriptor.UseDesignerByDefault);

            return new EditorInfo(editor.EditorFactory, descriptor.DisplayName, isDefaultEditor);
        }

        public async Task<bool> SetUseGlobalEditorAsync(string documentMoniker, bool useGlobalEditor)
        {
            Requires.NotNullOrEmpty(documentMoniker, nameof(documentMoniker));

            SubTypeDescriptor? editorInfo = await GetSubTypeDescriptorAsync(documentMoniker);
            if (editorInfo == null)
                return false;

            // 'useGlobalEditor' means use the default editor that is registered for source files
            await _options.Value.SetUseDesignerByDefaultAsync(editorInfo.DesignerCategoryForPersistence, !useGlobalEditor);
            return true;
        }

        private async Task<IProjectSpecificEditorInfo?> GetDefaultEditorAsync(string documentMoniker)
        {
            IProjectSpecificEditorProvider? defaultProvider = GetDefaultEditorProvider();
            if (defaultProvider == null)
                return null;

            return await defaultProvider.GetSpecificEditorAsync(documentMoniker);
        }

        private async Task<SubTypeDescriptor?> GetSubTypeDescriptorAsync(string documentMoniker)
        {
            string? subType = await GetSubTypeAsync(documentMoniker);
            if (subType != null)
            {
                foreach (SubTypeDescriptor descriptor in SubTypeDescriptors)
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
            if (item == null)
                return null;

            IRule browseObject = item.BrowseObjectProperties;
            if (browseObject == null)
                return null;

            if (!(browseObject.GetProperty(Compile.SubTypeProperty) is IEvaluatedProperty property))
                return null;

            return await property.GetEvaluatedValueAtEndAsync();
        }

        private async Task<IProjectItemTree?> FindCompileItemByMonikerAsync(string documentMoniker)
        {
            IProjectTreeServiceState result = await _projectTree.Value.TreeService.PublishAnyNonNullTreeAsync();

            if (result.TreeProvider.FindByPath(result.Tree, documentMoniker) is IProjectItemTree treeItem &&
                StringComparers.ItemTypes.Equals(treeItem.Item.ItemType, Compile.SchemaName))
            {
                return treeItem;
            }

            return null;
        }

        private IProjectSpecificEditorProvider? GetDefaultEditorProvider()
        {
            Lazy<IProjectSpecificEditorProvider> editorProvider = ProjectSpecificEditorProviders.FirstOrDefault(p => string.Equals(p.Metadata.Name, "Default", StringComparison.OrdinalIgnoreCase));

            return editorProvider?.Value;
        }
    }
}
