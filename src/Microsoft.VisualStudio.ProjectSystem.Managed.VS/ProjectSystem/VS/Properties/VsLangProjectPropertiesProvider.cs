// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    /// <summary>
    /// This provider imports <see cref="VSLangProj.VSProject"/> provided by CPS
    /// and wraps it into an object that implements both <see cref="VSLangProj.VSProject"/> and 
    /// <see cref="VSLangProj.ProjectProperties"/>. This enables us to provide
    /// ProjectProperties to the Project Property Pages and maintain Backward Compatibility.
    /// </summary>
    internal class VsLangProjectPropertiesProvider
    {
        private readonly VSProject _vsProject;
        private readonly IProjectThreadingService _threadingService;
        private readonly ActiveConfiguredProject<ProjectProperties> _projectProperties;

        [ImportingConstructor]
        public VsLangProjectPropertiesProvider(
            [Import(ExportContractNames.VsTypes.CpsVSProject)] VSProject vsProject,
            IProjectThreadingService threadingService,
            ActiveConfiguredProject<ProjectProperties> projectProperties)
        {
            Requires.NotNull(vsProject, nameof(vsProject));
            Requires.NotNull(threadingService, nameof(threadingService));
            Requires.NotNull(projectProperties, nameof(projectProperties));

            _vsProject = vsProject;
            _threadingService = threadingService;
            _projectProperties = projectProperties;
        }

        [Export(ExportContractNames.VsTypes.VSProject, typeof(VSProject))]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        [Order(10)]
        public VSProject VSProject
        {
            get
            {
                return new VsLangProjectProperties(_vsProject, _threadingService, _projectProperties);
            }
        }
    }
}
