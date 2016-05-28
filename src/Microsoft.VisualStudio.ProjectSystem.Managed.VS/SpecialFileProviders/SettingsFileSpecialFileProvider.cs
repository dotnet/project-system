using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Diagnostics.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.SpecialFileProviders
{
    [ExportSpecialFileProvider(SpecialFiles.AppSettings)]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class SettingsFileSpecialFileProvider : AbstractSpecialFileProvider
    {
        protected override string GetFileNameOfSpecialFile(SpecialFiles fileId)
        {
            Assert(fileId == SpecialFiles.AppSettings);
            return "Settings.settings";
        }

        protected override string GetTemplateForSpecialFile(SpecialFiles fileId)
        {
            Assert(fileId == SpecialFiles.AppSettings);
            return "SettingsInternal.zip";
        }
    }
}
