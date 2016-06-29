using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IProjectTreeExtensions
    {
        public static HierarchyId GetHierarchyId(this IProjectTree tree) =>
            new HierarchyId(tree.IsRoot() ? VSConstants.VSITEMID_ROOT : unchecked((uint)tree.Identity.ToInt32()));
    }
}
