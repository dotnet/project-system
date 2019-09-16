// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using VSLangProj110;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.VisualBasic
{
    [Export(ExportContractNames.VsTypes.ConfiguredProjectPropertiesAutomationObject)]
    [Order(Order.Default)]
    [AppliesTo(ProjectCapability.VisualBasic)]
    public class VisualBasicProjectConfigurationProperties : AbstractProjectConfigurationProperties,
        VBProjectConfigurationProperties6
    {
        [ImportingConstructor]
        internal VisualBasicProjectConfigurationProperties(
            ProjectProperties projectProperties,
            IProjectThreadingService threadingService)
            : base(projectProperties, threadingService)
        {
        }
    }
}
