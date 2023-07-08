// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [Export("UserFileWithXamlDefaultsWithInterception", typeof(IProjectPropertiesProvider))]
    [Export(typeof(IProjectPropertiesProvider))]
    [Export("UserFileWithXamlDefaultsWithInterception", typeof(IProjectInstancePropertiesProvider))]
    [Export(typeof(IProjectInstancePropertiesProvider))]
    [ExportMetadata("Name", "UserFileWithXamlDefaultsWithInterception")]
    [AppliesTo(ProjectCapability.ProjectPropertyInterception)]
    internal sealed class UserFileWithXamlDefaultsInterceptedProjectPropertiesProvider : UserFileInterceptedProjectPropertiesProvider
    {
        [ImportingConstructor]
        public UserFileWithXamlDefaultsInterceptedProjectPropertiesProvider(
            [Import(ContractNames.ProjectPropertyProviders.UserFileWithXamlDefaults)] IProjectPropertiesProvider provider,
            // We use project file here because in CPS, the UserFileWithXamlDefaults instance provider is implemented by the same
            // provider as the ProjectFile, and is exported as the ProjectFile provider.
            [Import(ContractNames.ProjectPropertyProviders.ProjectFile)] IProjectInstancePropertiesProvider instanceProvider,
            UnconfiguredProject project,
            [ImportMany(ContractNames.ProjectPropertyProviders.UserFileWithXamlDefaults)]IEnumerable<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> interceptingValueProviders)
            : base(provider, instanceProvider, project, interceptingValueProviders)
        {
        }
    }
}
