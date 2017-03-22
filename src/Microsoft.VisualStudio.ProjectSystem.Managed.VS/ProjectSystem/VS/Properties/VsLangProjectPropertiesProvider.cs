// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    internal class VsLangProjectPropertiesProvider : OnceInitializedOnceDisposed
    {
        private readonly VSProject _vsProject;
        private readonly ProjectProperties _projectProperties;

        private VSProject _vsLangProjectProperties;

        [ImportingConstructor]
        public VsLangProjectPropertiesProvider(
            [Import(ExportContractNames.VsTypes.CpsVSProject)] VSProject vsProject,
            ProjectProperties projectProperties)
        {
            _vsProject = vsProject;
            _projectProperties = projectProperties;
        }

        [Export(ExportContractNames.VsTypes.VSProject, typeof(VSProject))]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        public VSProject VSProject
        {
            get
            {
                EnsureInitialized();
                return _vsLangProjectProperties;
            }
        }

        protected override void Initialize()
        {
            _vsLangProjectProperties = new VsLangProjectProperties(_vsProject, _projectProperties);
        }

        protected override void Dispose(bool disposing)
        {
        }
    }
}
