// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties.Package
{
    // Sets the PackageIcon property to the path from the root of the package, and creates the
    // None item associated with the file on disk. The Include metadata of the None item is a relative
    // path to the file on disk from the project's directory. The None item includes 2 metadata elements
    // as Pack (set to True to be included in the package) and PackagePath (directory structure in the package
    // for the file to be placed). The PackageIcon property will use the directory indicated in PackagePath,
    // and the filename of the file in the Include filepath.
    //
    // Example:
    // <PropertyGroup>
    //   <PackageIcon>content\shell32_192.ico</PackageIcon>
    // </PropertyGroup>
    //
    // <ItemGroup>
    //   <None Include="..\..\..\shell32_192.ico">
    //     <Pack>True</Pack>
    //     <PackagePath>content</PackagePath>
    //   </None>
    // </ItemGroup>
    [ExportInterceptingPropertyValueProvider(PackageIconPropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class PackageIconValueProvider : InterceptingPropertyValueProviderBase
    {
        private const string PackageIconPropertyName = "PackageIcon";
        private const string PackMetadataName = "Pack";
        private const string PackagePathMetadataName = "PackagePath";

        private readonly IProjectItemProvider _sourceItemsProvider;
        private readonly UnconfiguredProject _unconfiguredProject;

        [ImportingConstructor]
        public PackageIconValueProvider(
            [Import(ExportContractNames.ProjectItemProviders.SourceFiles)] IProjectItemProvider sourceItemsProvider,
            UnconfiguredProject unconfiguredProject)
        {
            _sourceItemsProvider = sourceItemsProvider;
            _unconfiguredProject = unconfiguredProject;
        }

        private static bool IsForwardslashOrBackslash(string? path) =>
            "\\".Equals(path, StringComparisons.Paths) || "/".Equals(path, StringComparisons.Paths);

        // https://docs.microsoft.com/en-us/nuget/reference/msbuild-targets#packageicon
        private static string CreatePackageIcon(string filePath, string? packagePath)
        {
            string filename = Path.GetFileName(filePath);
            // Make a slash-only value into empty string, so it won't get prepended onto the path.
            // Also, make sure that the packagePath is non-null, so Path.Combine doesn't throw an ArgumentNullException.
            packagePath = IsForwardslashOrBackslash(packagePath) ? string.Empty : (packagePath ?? string.Empty);
            // The assumption is that packagePath does not contain a path to a file; only a directory path.
            return Path.Combine(packagePath, filename);
        }

        private async Task<IProjectItem?> GetExistingNoneItemAsync(string existingPackageIcon)
        {
            foreach (IProjectItem noneItem in await _sourceItemsProvider.GetItemsAsync(None.SchemaName))
            {
                string pack = await noneItem.Metadata.GetEvaluatedPropertyValueAsync(PackMetadataName);
                // Instead of doing pure equality between itemPackageIcon and existingPackageIcon, a user may update the PackagePath
                // of the item and forget to update the PackageIcon to reflect those changes, or vice versa. Instead, if the filename
                // of this packed None item and the filename of the PackageIcon match, consider those to be related to one another.
                if (bool.TryParse(pack, out bool packValue) && packValue &&
                    Path.GetFileName(noneItem.EvaluatedInclude).Equals(Path.GetFileName(existingPackageIcon), StringComparisons.PropertyLiteralValues))
                {
                    return noneItem;
                }
            }

            return null;
        }

        public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            string relativePath = PathHelper.MakeRelative(_unconfiguredProject, unevaluatedPropertyValue);
            string existingPackageIcon = await defaultProperties.GetEvaluatedPropertyValueAsync(PackageIconPropertyName);
            IProjectItem? existingItem = await GetExistingNoneItemAsync(existingPackageIcon);
            string packagePath = string.Empty;
            if (existingItem != null)
            {
                string itemPackagePath = await existingItem.Metadata.GetEvaluatedPropertyValueAsync(PackagePathMetadataName);
                // The new filepath is the same as the current. No item changes are required.
                if (relativePath.Equals(existingItem.EvaluatedInclude, StringComparisons.Paths))
                {
                    return CreatePackageIcon(existingItem.EvaluatedInclude, itemPackagePath);
                }

                await existingItem.SetUnevaluatedIncludeAsync(relativePath);
                packagePath = itemPackagePath;
            }
            else
            {
                await _sourceItemsProvider.AddAsync(None.SchemaName, relativePath, new Dictionary<string, string> { { PackMetadataName, bool.TrueString }, { PackagePathMetadataName, packagePath } });
            }

            return CreatePackageIcon(relativePath, packagePath);
        }

        private async Task<string> GetItemIncludeValueAsync(string propertyValue)
        {
            IProjectItem? existingItem = await GetExistingNoneItemAsync(propertyValue);
            return existingItem?.EvaluatedInclude ?? propertyValue;
        }

        public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties) =>
            GetItemIncludeValueAsync(unevaluatedPropertyValue);

        public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties) =>
            GetItemIncludeValueAsync(evaluatedPropertyValue);
    }
}
