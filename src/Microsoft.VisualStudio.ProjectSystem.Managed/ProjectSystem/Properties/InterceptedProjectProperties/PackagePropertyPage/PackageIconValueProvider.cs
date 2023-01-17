// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
    [ExportInterceptingPropertyValueProvider(PackageIconPropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class PackageIconValueProvider : PackageFilePropertyValueProviderBase
    {
        private const string PackageIconPropertyName = "PackageIcon";

        [ImportingConstructor]
        public PackageIconValueProvider(
            [Import(ExportContractNames.ProjectItemProviders.SourceFiles)] IProjectItemProvider sourceItemsProvider,
            UnconfiguredProject unconfiguredProject)
            : base(PackageIconPropertyName, sourceItemsProvider, unconfiguredProject)
        {
        }
    }
}
