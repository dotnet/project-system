// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    [Export("SourceFile", typeof(IProjectPropertiesProvider))]
    [Export(typeof(IProjectPropertiesProvider))]
    [ExportMetadata("Name", "SourceFile")]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class SourceFilePropertiesProvider : AbstractSourceFilePropertyProvider
    {
        [ImportingConstructor]
        public SourceFilePropertiesProvider(UnconfiguredProject unconfiguredProject,
                                            [Import("Microsoft.VisualStudio.ProjectSystem.ProjectFile")] IProjectPropertiesProvider defaultProjectFilePropertiesProvider,
                                            VisualStudioWorkspace workspace,
                                            IProjectThreadingService threadingService) :
            base(unconfiguredProject, defaultProjectFilePropertiesProvider, workspace, threadingService)
        {
        }
    }
}
