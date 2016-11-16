using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Generators
{
    [Export(ExportContractNames.VsTypes.ProjectNodeComExtension)]
    [AppliesTo(ProjectCapability.VisualBasic)]
    [ComServiceIid(typeof(IVsSingleFileGeneratorFactory))]
    class VisualBasicIVsSingleFileGeneratorFactoryAggregator : IVsSingleFileGeneratorFactoryAggregator
    {
        [ImportingConstructor]
        public VisualBasicIVsSingleFileGeneratorFactoryAggregator([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IVSRegistryHelper registryHelper) : base(serviceProvider, registryHelper)
        {
        }

        protected override Guid PackageGuid => Guid.Parse(VisualBasicProjectSystemPackage.ProjectTypeGuid);
    }
}
