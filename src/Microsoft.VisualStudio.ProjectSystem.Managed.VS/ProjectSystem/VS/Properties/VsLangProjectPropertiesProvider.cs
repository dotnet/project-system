// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    internal class VsLangProjectPropertiesProvider
    {
        private readonly VSProject _vsProject;
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly ActiveConfiguredProject<ProjectProperties> _projectProperties;

        [ImportingConstructor]
        public VsLangProjectPropertiesProvider(
            [Import(ExportContractNames.VsTypes.CpsVSProject)] VSProject vsProject,
            IUnconfiguredProjectVsServices projectVsServices,
            ActiveConfiguredProject<ProjectProperties> projectProperties)
        {
            Requires.NotNull(vsProject, nameof(vsProject));
            Requires.NotNull(projectVsServices, nameof(projectVsServices));
            Requires.NotNull(projectProperties, nameof(projectProperties));

            _vsProject = vsProject;
            _projectVsServices = projectVsServices;
            _projectProperties = projectProperties;
        }

        [Export(ExportContractNames.VsTypes.VSProject, typeof(VSProject))]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        [Order(10)]
        public VSProject VSProject
        {
            get
            {
                return new VsLangProjectProperties(_vsProject, _projectVsServices, _projectProperties);
            }
        }
    }
}
