using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [Export("UserFileWithXamlDefaultsWithInterception", typeof(IProjectPropertiesProvider))]
    [Export(typeof(IProjectPropertiesProvider))]
    [Export("UserFileWithXamlDefaultsWithInterception", typeof(IProjectInstancePropertiesProvider))]
    [Export(typeof(IProjectInstancePropertiesProvider))]
    [ExportMetadata("Name", "UserFileWithXamlDefaultsWithInterception")]
    [AppliesTo(ProjectCapability.AlwaysAvailable)]
    internal sealed class UserFileWithXamlDefaultsInterceptedProjectPropertiesProvider : InterceptedProjectPropertiesProviderBase
    {
        private const string userSuffix = ".user";

        public override string DefaultProjectPath {
            get { return base.DefaultProjectPath + userSuffix; }
        }

        [ImportingConstructor]
        public UserFileWithXamlDefaultsInterceptedProjectPropertiesProvider(
            [Import(ContractNames.ProjectPropertyProviders.UserFileWithXamlDefaults)] IProjectPropertiesProvider provider,
            [Import(ContractNames.ProjectPropertyProviders.ProjectFile)] IProjectInstancePropertiesProvider instanceProvider,
            UnconfiguredProject unconfiguredProject,
            [ImportMany(ContractNames.ProjectPropertyProviders.UserFile)]IEnumerable<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> interceptingValueProviders)
            : base(provider, instanceProvider, unconfiguredProject, interceptingValueProviders)
        {
        }
    }
}
