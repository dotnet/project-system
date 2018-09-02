using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    internal class UpToDateCheckItemComparer : IEqualityComparer<(string Path, string Link, CopyToOutputDirectoryType CopyType)>
    {
        public static UpToDateCheckItemComparer Instance = new UpToDateCheckItemComparer();

        private UpToDateCheckItemComparer()
        {
        }

        public bool Equals(
            (string Path, string Link, CopyToOutputDirectoryType CopyType) x,
            (string Path, string Link, CopyToOutputDirectoryType CopyType) y)
        {
            return StringComparers.Paths.Equals(x.Path, y.Path);
        }

        public int GetHashCode(
            (string Path, string Link, CopyToOutputDirectoryType CopyType) obj)
        {
            return StringComparers.Paths.GetHashCode(obj.Path);
        }
    }
}
