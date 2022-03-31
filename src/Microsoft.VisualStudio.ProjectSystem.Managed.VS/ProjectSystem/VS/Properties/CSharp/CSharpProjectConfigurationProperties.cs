// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using VSLangProj110;
using VSLangProj80;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.CSharp
{
    [Export(ExportContractNames.VsTypes.ConfiguredProjectPropertiesAutomationObject)]
    [Order(Order.Default)]
    [AppliesTo(ProjectCapability.CSharp)]
    public class CSharpProjectConfigurationProperties : AbstractProjectConfigurationProperties,
        CSharpProjectConfigurationProperties3,
        CSharpProjectConfigurationProperties6
    {
        [ImportingConstructor]
        internal CSharpProjectConfigurationProperties(
            ProjectProperties projectProperties,
            IProjectThreadingService threadingService)
            : base(projectProperties, threadingService)
        {
        }

        public string ErrorReport { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    }
}
