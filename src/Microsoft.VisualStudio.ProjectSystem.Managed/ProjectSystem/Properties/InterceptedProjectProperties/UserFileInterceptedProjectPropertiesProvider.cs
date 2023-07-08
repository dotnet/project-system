// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [Export("UserFileWithInterception", typeof(IProjectPropertiesProvider))]
    [Export(typeof(IProjectPropertiesProvider))]
    [Export("UserFileWithInterception", typeof(IProjectInstancePropertiesProvider))]
    [Export(typeof(IProjectInstancePropertiesProvider))]
    [ExportMetadata("Name", "UserFileWithInterception")]
    [AppliesTo(ProjectCapability.ProjectPropertyInterception)]
    internal class UserFileInterceptedProjectPropertiesProvider : InterceptedProjectPropertiesProviderBase
    {
        private const string UserSuffix = ".user";

        public override string DefaultProjectPath
        {
            get { return base.DefaultProjectPath + UserSuffix; }
        }

        [ImportingConstructor]
        public UserFileInterceptedProjectPropertiesProvider(
            [Import(ContractNames.ProjectPropertyProviders.UserFile)] IProjectPropertiesProvider provider,
            // We use project file here because in CPS, the UserFile instance provider is implemented by the same
            // provider as the ProjectFile, and is exported as the ProjectFile provider.
            [Import(ContractNames.ProjectPropertyProviders.ProjectFile)] IProjectInstancePropertiesProvider instanceProvider,
            UnconfiguredProject project,
            [ImportMany(ContractNames.ProjectPropertyProviders.UserFile)]IEnumerable<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> interceptingValueProviders)
            : base(provider, instanceProvider, project, interceptingValueProviders)
        {
        }
    }
}
