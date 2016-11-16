using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Generators
{
    [Export(ExportContractNames.VsTypes.ProjectNodeComExtension)]
    [AppliesTo(ProjectCapability.CSharp)]
    [ComServiceIid(typeof(IVsSingleFileGeneratorFactory))]
    class CSharpIVsSingleFileGeneratorFactoryAggregator : IVsSingleFileGeneratorFactoryAggregator
    {
        [ImportingConstructor]
        public CSharpIVsSingleFileGeneratorFactoryAggregator([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IVSRegistryHelper registryHelper) : base(serviceProvider, registryHelper)
        {
        }

        protected override Guid PackageGuid => Guid.Parse(CSharpProjectSystemPackage.ProjectTypeGuid);
    }
}
