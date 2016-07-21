using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [Export("UserFileWithInterception", typeof(IProjectPropertiesProvider))]
    [ExportMetadata("Name", "UserFileWithInterception")]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    class UserFileInterceptedProjectPropertiesProvider : InterceptedProjectPropertiesProviderBase
    {
        [ImportingConstructor]
        public UserFileInterceptedProjectPropertiesProvider(
            [Import(ContractNames.ProjectPropertyProviders.UserFile)] IProjectPropertiesProvider provider,
            UnconfiguredProject unconfiguredProject,
            [ImportMany(ContractNames.ProjectPropertyProviders.UserFile)]IEnumerable<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> interceptingValueProviders)
            : base(provider, unconfiguredProject, interceptingValueProviders)
        {
        }
    }
}
