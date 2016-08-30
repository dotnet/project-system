using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Mocks
{
    public static class IVsStartupProjectsListServiceFactory
    {
        public static Mock<IVsStartupProjectsListService> CreateMockInstance(Guid projectGuid)
        {
            var iVsStartupProjectsListService = new Mock<IVsStartupProjectsListService>();

            iVsStartupProjectsListService.Setup(s => s.AddProject(ref projectGuid));
            iVsStartupProjectsListService.Setup(s => s.RemoveProject(ref projectGuid));

            return iVsStartupProjectsListService;
        }
    }
}
