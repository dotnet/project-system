// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [Export("ProjectFileWithInterception", typeof(IProjectPropertiesProvider))]
    [Export(typeof(IProjectPropertiesProvider))]
    [Export("ProjectFileWithInterception", typeof(IProjectInstancePropertiesProvider))]
    [Export(typeof(IProjectInstancePropertiesProvider))]
    [ExportMetadata("Name", "ProjectFileWithInterception")]
    [AppliesTo(ProjectCapability.ProjectPropertyInterception)]
    internal sealed class ProjectFileInterceptedProjectPropertiesProvider : InterceptedProjectPropertiesProviderBase
    {
        [ImportingConstructor]
        public ProjectFileInterceptedProjectPropertiesProvider(
            [Import(ContractNames.ProjectPropertyProviders.ProjectFile)] IProjectPropertiesProvider provider,
            [Import(ContractNames.ProjectPropertyProviders.ProjectFile)] IProjectInstancePropertiesProvider instanceProvider,
            UnconfiguredProject project,
            [ImportMany(ContractNames.ProjectPropertyProviders.ProjectFile)]IEnumerable<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> interceptingValueProviders)
            : base(provider, instanceProvider, project, interceptingValueProviders)
        {
        }
    }
}
