// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IO;

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
    //   <PackageIcon>content\shell32_192.png</PackageIcon>
    // </PropertyGroup>
    //
    // <ItemGroup>
    //   <None Include="..\..\..\shell32_192.png">
    //     <Pack>True</Pack>
    //     <PackagePath>content</PackagePath>
    //   </None>
    // </ItemGroup>
    [ExportInterceptingPropertyValueProvider(ApplicationIconPropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class ApplicationIconValueProvider : InterceptingPropertyValueProviderBase
    {
        private const string ApplicationIconPropertyName = "ApplicationIcon";

        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IFileSystem _fileSystem;

        [ImportingConstructor]
        public ApplicationIconValueProvider(UnconfiguredProject unconfiguredProject, IFileSystem fileSystem)
        {
            _unconfiguredProject = unconfiguredProject;
            _fileSystem = fileSystem;
        }

        // TODO: Check if we need to delete previous file
        // TODO: Check if file is already imported as a none/content item
        public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            // Allows the user to clear the application icon.
            if (string.IsNullOrEmpty(unevaluatedPropertyValue))
            {
                return null;
            }

            // If selected path is invalid, ignore and return previous value.
            if (!File.Exists(unevaluatedPropertyValue))
            {
                return await defaultProperties.GetUnevaluatedPropertyValueAsync(propertyName);
            }

            // Copy the file into the project directory.
            // This isn't a limitation of ApplicationIcon; it is to simply mimic the legacy property page interaction.
            if (_unconfiguredProject.IsOutsideProjectDirectory(unevaluatedPropertyValue))
            {
                string fileName = Path.GetFileName(unevaluatedPropertyValue);
                string destination = Path.Combine(Path.GetDirectoryName(_unconfiguredProject.FullPath), fileName);
                _fileSystem.CopyFile(unevaluatedPropertyValue, destination, overwrite: true);
                return fileName;
            }

            return PathHelper.MakeRelative(_unconfiguredProject, unevaluatedPropertyValue);

            //string relativePath = PathHelper.MakeRelative(_unconfiguredProject, unevaluatedPropertyValue);
            //string existingPropertyValue = await defaultProperties.GetEvaluatedPropertyValueAsync(PackageIconPropertyName);
            //IProjectItem? existingItem = await GetExistingNoneItemAsync(existingPropertyValue);
            //string packagePath = string.Empty;
            //if (existingItem != null)
            //{
            //    packagePath = await existingItem.Metadata.GetEvaluatedPropertyValueAsync(PackagePathMetadataName);
            //    // The new filepath is the same as the current. No item changes are required.
            //    if (relativePath.Equals(existingItem.EvaluatedInclude, StringComparisons.Paths))
            //    {
            //        return CreatePackageIcon(existingItem.EvaluatedInclude, packagePath);
            //    }
            //}

            //// None items outside of the project file cannot be updated.
            //if (existingItem?.PropertiesContext.IsProjectFile ?? false)
            //{
            //    await existingItem.SetUnevaluatedIncludeAsync(relativePath);
            //}
            //else
            //{
            //    await _sourceItemsProvider.AddAsync(None.SchemaName, relativePath, new Dictionary<string, string> { { PackMetadataName, bool.TrueString }, { PackagePathMetadataName, packagePath } });
            //}

            //return CreatePackageIcon(relativePath, packagePath);
        }
    }
}
