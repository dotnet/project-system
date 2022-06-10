// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Execution;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [Export("ProjectFileWithInterceptionViaSnapshot", typeof(IProjectPropertiesProvider))]
    [Export(typeof(IProjectPropertiesProvider))]
    [Export("ProjectFileWithInterceptionViaSnapshot", typeof(IProjectInstancePropertiesProvider))]
    [Export(typeof(IProjectInstancePropertiesProvider))]
    [ExportMetadata("Name", "ProjectFileWithInterceptionViaSnapshot")]
    [ExportMetadata("HasEquivalentProjectInstancePropertiesProvider", true)]
    [AppliesTo(ProjectCapability.ProjectPropertyInterception)]
    internal sealed class ProjectFileInterceptedViaSnapshotProjectPropertiesProvider : InterceptedProjectPropertiesProviderBase
    {
        [ImportingConstructor]
        public ProjectFileInterceptedViaSnapshotProjectPropertiesProvider(
            [Import(ContractNames.ProjectPropertyProviders.ProjectFile)] IProjectPropertiesProvider provider,
            [Import(ContractNames.ProjectPropertyProviders.ProjectFile)] IProjectInstancePropertiesProvider instanceProvider,
            UnconfiguredProject project,
            [ImportMany(ContractNames.ProjectPropertyProviders.ProjectFile)]IEnumerable<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> interceptingValueProviders)
            : base(provider, instanceProvider, project, interceptingValueProviders)
        {
        }

        public override IProjectProperties GetCommonProperties()
        {
            IProjectProperties defaultProperties = base.GetCommonProperties();
            return InterceptProperties(defaultProperties);
        }

        public override IProjectProperties GetCommonProperties(ProjectInstance projectInstance)
        {
            IProjectProperties defaultProperties = base.GetCommonProperties(projectInstance);
            return InterceptProperties(defaultProperties);
        }
    }
}
