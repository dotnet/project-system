// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;

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
