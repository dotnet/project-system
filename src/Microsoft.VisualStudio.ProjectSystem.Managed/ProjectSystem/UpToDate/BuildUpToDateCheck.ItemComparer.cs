using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    internal sealed partial class BuildUpToDateCheck
    {
        private sealed class ItemComparer : IEqualityComparer<(string path, string? link, CopyType copyType)>
        {
            public static readonly ItemComparer Instance = new ItemComparer();

            private ItemComparer()
            {
            }

            public bool Equals(
                (string path, string? link, CopyType copyType) x,
                (string path, string? link, CopyType copyType) y)
            {
                return StringComparers.Paths.Equals(x.path, y.path);
            }

            public int GetHashCode(
                (string path, string? link, CopyType copyType) obj)
            {
                return StringComparers.Paths.GetHashCode(obj.path);
            }
        }
    }
}
