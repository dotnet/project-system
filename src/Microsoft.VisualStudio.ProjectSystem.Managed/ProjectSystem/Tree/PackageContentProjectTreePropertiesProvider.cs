// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Tree
{
    /// <summary>
    ///     Marks content items that come from packages as "user read-only".
    /// </summary>
    [Export(typeof(IProjectTreePropertiesProvider))]
    [AppliesTo(ProjectCapability.PackageReferences)]
    internal class PackageContentProjectTreePropertiesProvider : IProjectTreePropertiesProvider
    {
        public void CalculatePropertyValues(IProjectTreeCustomizablePropertyContext propertyContext, IProjectTreeCustomizablePropertyValues propertyValues)
        {
            // Package content items always come in as linked items, so to reduce
            // the number of items we look at, we limit ourselves to them
            if (propertyValues.Flags.Contains(ProjectTreeFlags.LinkedItem) &&
                propertyContext.Metadata is not null &&
                propertyContext.Metadata.TryGetValue(None.NuGetPackageIdProperty, out string packageId) && packageId.Length > 0)
            {
                propertyValues.Flags |= ProjectTreeFlags.UserReadOnly;
            }
        }
    }
}
