// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties.Package
{
    // Sets the provided property to the path from the root of the package, and creates the
    // None item associated with the file on disk. The Include metadata of the None item is a relative
    // path to the file on disk from the project's directory. The None item includes 2 metadata elements
    // as Pack (set to True to be included in the package) and PackagePath (directory structure in the package
    // for the file to be placed). The provided property will use the directory indicated in PackagePath,
    // and the filename of the file in the Include filepath.
    //
    // Example:
    // <PropertyGroup>
    //   <PackageIcon>content\shell32_192.png</PackageIcon>
    // </PropertyGroup>
    //
    // <ItemGroup>
    //   <None Include="..\..\..\shell32_192.png">
    //     <Pack>True</Pack>
    //     <PackagePath>content</PackagePath>
    //   </None>
    // </ItemGroup>
    internal abstract class PackageFilePropertyValueProviderBase : InterceptingPropertyValueProviderBase
    {
        private const string PackMetadataName = "Pack";
        private const string PackagePathMetadataName = "PackagePath";

        private readonly IProjectItemProvider _sourceItemsProvider;
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly string _propertyName;

        protected PackageFilePropertyValueProviderBase(string propertyName, IProjectItemProvider sourceItemsProvider, UnconfiguredProject unconfiguredProject)
        {
            _propertyName = propertyName;
            _sourceItemsProvider = sourceItemsProvider;
            _unconfiguredProject = unconfiguredProject;
        }

        // https://docs.microsoft.com/en-us/nuget/reference/msbuild-targets#pack-scenarios
        private static string CreatePropertyValue(string filePath, string packagePath)
        {
            string filename = Path.GetFileName(filePath);
            // Make a slash-only value into empty string, so it won't get prepended onto the path.
            if (@"\".Equals(packagePath, StringComparisons.Paths) || "/".Equals(packagePath, StringComparisons.Paths))
            {
                packagePath = string.Empty;
            }
            // The assumption is that packagePath does not contain a path to a file; only a directory path.
            return Path.Combine(packagePath, filename);
        }

        private async Task<IProjectItem?> GetExistingNoneItemAsync(string existingPropertyValue)
        {
            return await _sourceItemsProvider.GetItemAsync(None.SchemaName, async ni =>
            {
                string pack = await ni.Metadata.GetEvaluatedPropertyValueAsync(PackMetadataName);
                // Instead of doing pure equality between a calcuated item's property value and the existing property value,
                // a user may update the PackagePath of the item and forget to update the property's value to reflect those changes, or vice versa.
                // If the filename of this packed None item and the filename of the property's value match, consider those to be related to one another.
                return bool.TryParse(pack, out bool packValue) && packValue &&
                    Path.GetFileName(ni.EvaluatedInclude).Equals(Path.GetFileName(existingPropertyValue), StringComparisons.PropertyLiteralValues);
            });
        }

        public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            bool isEmptyValue = string.IsNullOrEmpty(unevaluatedPropertyValue);
            string relativePath = !isEmptyValue ? PathHelper.MakeRelative(_unconfiguredProject, unevaluatedPropertyValue) : string.Empty;
            string existingPropertyValue = await defaultProperties.GetEvaluatedPropertyValueAsync(_propertyName);
            IProjectItem? existingItem = await GetExistingNoneItemAsync(existingPropertyValue);
            // This default was string.Empty but caused issues for files already part of the project via globbing in the AddAsync call. It would not add the necessary
            // <PackagePath></PackagePath> node, which causes the value during pack to default to "content;contentFiles" which is not the intended directory.
            // See discussion here for more details: https://github.com/dotnet/project-system/issues/7642
            string packagePath = @"\";
            if (existingItem is not null)
            {
                packagePath = await existingItem.Metadata.GetEvaluatedPropertyValueAsync(PackagePathMetadataName);
                // The new filepath is the same as the current. No item changes are required.
                if (relativePath.Equals(existingItem.EvaluatedInclude, StringComparisons.Paths))
                {
                    return CreatePropertyValue(existingItem.EvaluatedInclude, packagePath);
                }
            }

            // None items outside of the project file cannot be updated.
            if (existingItem?.PropertiesContext?.IsProjectFile ?? false)
            {
                if (!isEmptyValue)
                {
                    await existingItem.SetUnevaluatedIncludeAsync(relativePath);
                }
                else
                {
                    await existingItem.RemoveAsync();
                }
            }
            else
            {
                await _sourceItemsProvider.AddAsync(None.SchemaName, relativePath, new Dictionary<string, string>
                {
                    { PackMetadataName, bool.TrueString },
                    { PackagePathMetadataName, packagePath }
                });
            }

            return !isEmptyValue ? CreatePropertyValue(relativePath, packagePath) : string.Empty;
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
