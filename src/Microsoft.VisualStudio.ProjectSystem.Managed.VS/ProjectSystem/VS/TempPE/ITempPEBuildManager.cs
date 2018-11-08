using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    internal interface ITempPEBuildManager
    {
        Task<string[]> GetDesignTimeOutputFilenamesAsync(bool shared);
        Task<string> GetTempPEBlobAsync(string bstrOutputMoniker);
    }
}
