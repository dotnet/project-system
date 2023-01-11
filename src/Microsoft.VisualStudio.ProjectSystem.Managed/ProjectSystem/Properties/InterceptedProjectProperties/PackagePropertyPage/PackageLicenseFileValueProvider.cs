// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties.Package
{
    // Sets the PackageLicenseFile property to the path from the root of the package, and creates the
    // None item associated with the file on disk. The Include metadata of the None item is a relative
    // path to the file on disk from the project's directory. The None item includes 2 metadata elements
    // as Pack (set to True to be included in the package) and PackagePath (directory structure in the package
    // for the file to be placed). The PackageLicenseFile property will use the directory indicated in PackagePath,
    // and the filename of the file in the Include filepath.
    //
    // Example:
    // <PropertyGroup>
    //   <PackageLicenseFile>docs\LICENSE.txt</PackageLicenseFile>
    // </PropertyGroup>
    //
    // <ItemGroup>
    //   <None Include="..\..\..\LICENSE.txt">
    //     <Pack>True</Pack>
    //     <PackagePath>docs</PackagePath>
    //   </None>
    // </ItemGroup>
    [ExportInterceptingPropertyValueProvider(PackageLicenseFilePropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class PackageLicenseFileValueProvider : PackageFilePropertyValueProviderBase
    {
        private const string PackageLicenseFilePropertyName = "PackageLicenseFile";

        [ImportingConstructor]
        public PackageLicenseFileValueProvider(
            [Import(ExportContractNames.ProjectItemProviders.SourceFiles)] IProjectItemProvider sourceItemsProvider,
            UnconfiguredProject unconfiguredProject)
            : base(PackageLicenseFilePropertyName, sourceItemsProvider, unconfiguredProject)
        {
        }
    }
}
