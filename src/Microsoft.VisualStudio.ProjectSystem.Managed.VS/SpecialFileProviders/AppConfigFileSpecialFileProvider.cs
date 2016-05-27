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
    [ExportSpecialFileProvider(SpecialFiles.AppConfig)]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class AppConfigFileSpecialFileProvider : AbstractSpecialFileProvider
    {
        protected override string GetFileNameOfSpecialFile(SpecialFiles fileId)
        {
            Assert(fileId == SpecialFiles.AppConfig);
            return "App.config";
        }

        protected override string GetTemplateForSpecialFile(SpecialFiles fileId)
        {
            Assert(fileId == SpecialFiles.AppConfig);
            return "AppConfigurationInternal.zip";
        }

        protected override bool ShouldLookInAppDesignerFolder => false;
    }
}
