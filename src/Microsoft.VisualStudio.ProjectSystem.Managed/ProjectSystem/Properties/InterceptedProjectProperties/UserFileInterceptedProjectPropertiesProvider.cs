using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [Export("UserFileWithInterception", typeof(IProjectPropertiesProvider))]
    [Export(typeof(IProjectPropertiesProvider))]
    [Export("UserFileWithInterception", typeof(IProjectInstancePropertiesProvider))]
    [Export(typeof(IProjectInstancePropertiesProvider))]
    [ExportMetadata("Name", "UserFileWithInterception")]
    [AppliesTo(ProjectCapability.AlwaysAvailable)]
    internal class UserFileInterceptedProjectPropertiesProvider : InterceptedProjectPropertiesProviderBase
    {
        private const string userSuffix = ".user";

        public override string DefaultProjectPath {
            get { return base.DefaultProjectPath + userSuffix; }
        }

        [ImportingConstructor]
        public UserFileInterceptedProjectPropertiesProvider(
            [Import(ContractNames.ProjectPropertyProviders.UserFile)] IProjectPropertiesProvider provider,
            // We use project file here because in CPS, the UserFile instance provider is implemented by the same
            // provider as the ProjectFile, and is exported as the ProjectFile provider.
            [Import(ContractNames.ProjectPropertyProviders.ProjectFile)] IProjectInstancePropertiesProvider instanceProvider,
            UnconfiguredProject unconfiguredProject,
            [ImportMany(ContractNames.ProjectPropertyProviders.UserFile)]IEnumerable<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> interceptingValueProviders)
            : base(provider, instanceProvider, unconfiguredProject, interceptingValueProviders)
        {
        }
    }
}
