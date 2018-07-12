using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    internal class UpToDateCheckItemComparer : IEqualityComparer<(string, string, CopyToOutputDirectoryType)>
    {
        public static UpToDateCheckItemComparer Instance = new UpToDateCheckItemComparer();

        private UpToDateCheckItemComparer()
        {
        }

        public bool Equals((string, string, CopyToOutputDirectoryType) x, (string, string, CopyToOutputDirectoryType) y) => StringComparers.Paths.Equals(x.Item1, y.Item1);

        public int GetHashCode((string, string, CopyToOutputDirectoryType) obj) => StringComparers.Paths.GetHashCode(obj.Item1);
    }
}
