using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    
    internal static class IPhysicalProjectTreeExtensions
    {
        public static bool NodeCanHaveAdditions(this IPhysicalProjectTree tree, IProjectTree node) =>
            tree.TreeProvider.GetAddNewItemDirectory(node) != null;

    }
}
