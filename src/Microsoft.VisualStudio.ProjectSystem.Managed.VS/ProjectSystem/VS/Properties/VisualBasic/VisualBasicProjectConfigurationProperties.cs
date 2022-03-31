// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
