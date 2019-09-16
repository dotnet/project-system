// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
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
