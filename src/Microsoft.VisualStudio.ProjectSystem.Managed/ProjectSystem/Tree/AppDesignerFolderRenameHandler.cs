// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using ManagedPriorityOrder = Microsoft.VisualStudio.ProjectSystem.Order;

namespace Microsoft.VisualStudio.ProjectSystem.Tree
{
    /// <summary>
    ///     Responsible for setting the AppDesignerFolder property for when the AppDesigner
    ///     folder ("Properties" in C# and "My Project" in Visual Basic) is renamed.
    /// </summary>
    [Order(ManagedPriorityOrder.Default)]
    [Export(typeof(IProjectTreeActionHandler))]
    [AppliesTo(ProjectCapability.AppDesigner)]
    internal class AppDesignerFolderRenameHandler : ProjectTreeActionHandlerBase
    {
        private readonly IActiveConfiguredValue<ProjectProperties> _properties;

        [ImportingConstructor]
        public AppDesignerFolderRenameHandler(IActiveConfiguredValue<ProjectProperties> properties)
        {
            _properties = properties;
        }

        public override async Task RenameAsync(IProjectTreeActionHandlerContext context, IProjectTree node, string value)
        {
            await base.RenameAsync(context, node, value);

            if (node.Flags.Contains(ProjectTreeFlags.AppDesignerFolder))
            {
                AppDesigner appDesigner = await _properties.Value.GetAppDesignerPropertiesAsync();

                await appDesigner.FolderName.SetUnevaluatedValueAsync(value);
            }
        }
    }
}
