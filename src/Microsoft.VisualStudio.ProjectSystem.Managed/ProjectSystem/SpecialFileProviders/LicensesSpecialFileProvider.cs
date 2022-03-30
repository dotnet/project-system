// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    /// <summary>
    ///     Provides a <see cref="ISpecialFileProvider"/> that handles the 'licenses.licx' file;
    ///     a file that contains a list of licensed (typically Windows Forms) components used by
    ///     a project and is typically found under the 'AppDesigner' folder.
    /// </summary>
    [ExportSpecialFileProvider(SpecialFiles.Licenses)]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class LicensesSpecialFileProvider : AbstractFindByNameUnderAppDesignerSpecialFileProvider
    {
        [ImportingConstructor]
        public LicensesSpecialFileProvider(ISpecialFilesManager specialFilesManager, IPhysicalProjectTree projectTree)
            : base("licenses.licx", specialFilesManager, projectTree)
        {
        }
    }
}
