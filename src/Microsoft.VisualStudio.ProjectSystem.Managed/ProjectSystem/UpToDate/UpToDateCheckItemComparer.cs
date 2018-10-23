using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    internal class UpToDateCheckItemComparer : IEqualityComparer<(string path, string link, CopyToOutputDirectoryType copyType)>
    {
        public static UpToDateCheckItemComparer Instance = new UpToDateCheckItemComparer();

        private UpToDateCheckItemComparer()
        {
        }

        public bool Equals(
            (string path, string link, CopyToOutputDirectoryType copyType) x,
            (string path, string link, CopyToOutputDirectoryType copyType) y)
        {
            return StringComparers.Paths.Equals(x.path, y.path);
        }

        public int GetHashCode(
            (string path, string link, CopyToOutputDirectoryType copyType) obj)
        {
            return StringComparers.Paths.GetHashCode(obj.path);
        }
    }
}
