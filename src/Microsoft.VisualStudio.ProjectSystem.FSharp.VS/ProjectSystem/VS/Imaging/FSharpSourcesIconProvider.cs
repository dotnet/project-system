using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;

using Microsoft.VisualStudio.Imaging;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Imaging
{
    [Export(typeof(IProjectTreePropertiesProvider))]
    [AppliesTo(ProjectCapability.FSharp)]
    [Order(Order.Default)]
    internal class FSharpSourcesIconProvider : IProjectTreePropertiesProvider
    {
        private static readonly Dictionary<string, ProjectImageMoniker> s_fileExtensionImageMap = new Dictionary<string, ProjectImageMoniker>(StringComparer.OrdinalIgnoreCase)
        {
            { ".fs",   KnownMonikers.FSFileNode.ToProjectSystemType() },
            { ".fsi",  KnownMonikers.FSSignatureFile.ToProjectSystemType() },
            { ".fsx",  KnownMonikers.FSScript.ToProjectSystemType() }
        };

        public void CalculatePropertyValues(IProjectTreeCustomizablePropertyContext propertyContext, IProjectTreeCustomizablePropertyValues propertyValues)
        {
            if (!propertyValues.Flags.Contains(ProjectTreeFlags.Common.Folder))
            {
                if (!propertyValues.Flags.Contains(ProjectTreeFlags.Common.SourceFile | ProjectTreeFlags.Common.FileSystemEntity))
                {
                    return;
                }

                propertyValues.Icon = GetIconForItem(propertyContext.ItemName);
            }
        }

        private static ProjectImageMoniker GetIconForItem(string itemName)
        {
            if (s_fileExtensionImageMap.TryGetValue(Path.GetExtension(itemName), out ProjectImageMoniker moniker))
            {
                return moniker;
            }

            // Return null so VS can supply the default icons.
            return null;
        }
    }
}
