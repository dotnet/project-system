using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [Export("ProjectFileWithInterception", typeof(IProjectPropertiesProvider))]
    [Export(typeof(IProjectPropertiesProvider))]
    [ExportMetadata("Name", "ProjectFileWithInterception")]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal sealed class ProjectFileInterceptedProjectPropertiesProvider : InterceptedProjectPropertiesProviderBase
    {
        [ImportingConstructor]
        public ProjectFileInterceptedProjectPropertiesProvider(
            [Import(ContractNames.ProjectPropertyProviders.ProjectFile)] IProjectPropertiesProvider provider,
            UnconfiguredProject unconfiguredProject,
            [ImportMany(ContractNames.ProjectPropertyProviders.ProjectFile)]IEnumerable<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> interceptingValueProviders)
            : base(provider, unconfiguredProject, interceptingValueProviders)
        {
        }
    }
}
