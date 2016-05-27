using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Diagnostics.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.ProjectSystem.SpecialFileProviders
{
    [ExportSpecialFileProvider(SpecialFiles.AssemblyResource)]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class ResourcesFileSpecialFileProvider : AbstractSpecialFileProvider
    {
        protected override string GetFileNameOfSpecialFile(SpecialFiles fileId)
        {
            Assert(fileId == SpecialFiles.AssemblyResource);
            return "Resources.resx";
        }

        protected override string GetTemplateForSpecialFile(SpecialFiles fileId)
        {
            Assert(fileId == SpecialFiles.AssemblyResource);
            return "ResourceInternal.zip";
        }
    }
}
