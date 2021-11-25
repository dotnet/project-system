// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem;

using ImageMoniker = Microsoft.VisualStudio.Imaging.Interop.ImageMoniker;

[assembly: DebuggerDisplay("{Microsoft.VisualStudio.ProjectSystem.Imaging.ImageMonikerDebuggerDisplay.FromImageMoniker(this)}", Target = typeof(ImageMoniker))]
[assembly: DebuggerDisplay("{Microsoft.VisualStudio.ProjectSystem.Imaging.ImageMonikerDebuggerDisplay.FromProjectImageMoniker(this)}", Target = typeof(ProjectImageMoniker))]

namespace Microsoft.VisualStudio.ProjectSystem.Imaging
{
    /// <summary>
    ///     Provides a friendly display for <see cref="ProjectImageMoniker"/> and <see cref="ImageMoniker"/> instances
    ///     based on the well known values from <see cref="KnownImageIds"/>.
    /// </summary>
    internal static class ImageMonikerDebuggerDisplay
    {
        private static readonly Dictionary<int, string> s_displayNames = CalculateDisplayNames();

        internal static string FromImageMoniker(ImageMoniker moniker) => DebugDisplay(moniker.Guid, moniker.Id);

        internal static string FromProjectImageMoniker(ProjectImageMoniker moniker) => DebugDisplay(moniker.Guid, moniker.Id);

        private static string DebugDisplay(Guid guid, int id)
        {
            if (guid == KnownImageIds.ImageCatalogGuid  && 
                s_displayNames.TryGetValue(id, out string displayName))
            {
                return displayName;
            }

            return $"{guid} ({id})";
        }

        private static Dictionary<int, string> CalculateDisplayNames()
        {
            Type type = typeof(KnownImageIds);

            return type.GetFields()
                       .Where(field => field.IsLiteral && field.FieldType == typeof(int))
                       .ToDictionary(field =>
                            (int)field.GetRawConstantValue(),
                            field => $"{type.Name}.{field.Name}");
        }
    }
}
