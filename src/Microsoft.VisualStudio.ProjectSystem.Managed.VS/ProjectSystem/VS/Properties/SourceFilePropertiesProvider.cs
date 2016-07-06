// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    /// <summary>
    /// A provider for properties that are stored in the source code of the project.
    /// This is defined in the VS layer so that we can import <see cref="VisualStudioWorkspace"/>.
    /// </summary>
    [Export("SourceFile", typeof(IProjectPropertiesProvider))]
    [Export(typeof(IProjectPropertiesProvider))]
    [ExportMetadata("Name", "SourceFile")]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class SourceFilePropertiesProvider : AbstractSourceFilePropertiesProvider
    {
        [ImportingConstructor]
        public SourceFilePropertiesProvider(UnconfiguredProject unconfiguredProject,
                                            VisualStudioWorkspace workspace,
                                            IProjectThreadingService threadingService) :
            base(unconfiguredProject, workspace, threadingService)
        {
        }
    }
}
