// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties.Package
{
    [ExportInterceptingPropertyValueProvider(PackageIconPropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class PackageIconValueProvider : InterceptingPropertyValueProviderBase
    {
        internal const string PackageIconPropertyName = "PackageIcon";
        private const string PackPropertyName = "Pack";
        private const string PackagePathPropertyName = "PackagePath";
        private const string NoneItem = "None";
        private const string TrueValue = "True";

        private readonly IProjectItemProvider _sourceItemsProvider;
        private readonly UnconfiguredProject _unconfiguredProject;

        //private string? _currentRelativePath;

        [ImportingConstructor]
        public PackageIconValueProvider(
            [Import(ExportContractNames.ProjectItemProviders.SourceFiles)] IProjectItemProvider sourceItemsProvider,
            UnconfiguredProject unconfiguredProject)
        {
            _sourceItemsProvider = sourceItemsProvider;
            _unconfiguredProject = unconfiguredProject;
        }

        // https://docs.microsoft.com/en-us/nuget/reference/msbuild-targets#packageicon
        private static string CreatePackageIcon(string filePath, string? packagePath)
        {
            string filename = Path.GetFileName(filePath);
            packagePath = ("\\".Equals(packagePath, StringComparison.Ordinal) || "/".Equals(packagePath, StringComparison.Ordinal))
                ? string.Empty : (packagePath ?? string.Empty);
            // The assumption is that PackagePath does not contain a path to a file; only a directory path.
            return Path.Combine(packagePath, filename);
        }

        public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            //string filename = PathHelper.TryMakeRelativeToProjectDirectory()
            //if (string.Equals(unevaluatedPropertyValue, NoneValue))
            //{
            //    await defaultProperties.DeletePropertyAsync(NeutralLanguagePropertyName);
            //    return null;
            //}
            //await _projectAccessor.EnterWriteLockAsync((pc, ct) => Task.CompletedTask);
            //await Task.CompletedTask;

            string relativePath = PathHelper.MakeRelative(_unconfiguredProject, unevaluatedPropertyValue);
            //string filename = Path.GetFileName(unevaluatedPropertyValue);

            // TODO: Need to use evaluated value of PackagePath... need to check this.
            // TODO: Need to calculate the PackageIcon based on PackagePath and file name... for matching.

            string? existingPackageIcon = await defaultProperties.GetEvaluatedPropertyValueAsync(PackageIconPropertyName);
            IProjectItem? existingItem = null;
            string packagePath = string.Empty;
            foreach (IProjectItem noneItem in await _sourceItemsProvider.GetItemsAsync(NoneItem))
            {
                string? pack = await noneItem.Metadata.GetEvaluatedPropertyValueAsync(PackPropertyName);
                if (TrueValue.Equals(pack, StringComparison.OrdinalIgnoreCase))
                {
                    string? itemPackagePath = await noneItem.Metadata.GetEvaluatedPropertyValueAsync(PackagePathPropertyName);
                    string itemPackageIcon = CreatePackageIcon(noneItem.EvaluatedInclude, itemPackagePath);

                    // If this is true, this item is the None item related to the PackageIcon.
                    if (itemPackageIcon.Equals(existingPackageIcon, StringComparison.Ordinal))
                    {
                        // The new filepath is the same as the current. No item changes are required.
                        if (relativePath.Equals(noneItem.EvaluatedInclude, StringComparison.Ordinal))
                        {
                            return itemPackageIcon;
                        }

                        existingItem = noneItem;
                        packagePath = itemPackagePath;
                        break;
                    }
                }
            }

            //if (relativePath.Equals(_currentRelativePath, StringComparison.Ordinal))
            //{
            //    return filename;
            //}

            //if (_currentRelativePath != null)
            //{
            //    var currentItem = await _sourceItemsProvider.FindItemByNameAsync(_currentRelativePath);
            //    if(currentItem != null && NoneItem.Equals(currentItem?.ItemType, StringComparison.Ordinal))
            //    {
            //        await _sourceItemsProvider.RemoveAsync(currentItem!);
            //    }
            //}

            if (existingItem != null)
            {
                await existingItem.SetUnevaluatedIncludeAsync(relativePath);
            }
            else
            {
                await _sourceItemsProvider.AddAsync(NoneItem, relativePath, new Dictionary<string, string> { { PackPropertyName, "True" }, { PackagePathPropertyName, packagePath } });
            }


            //if(PathHelper.TryMakeRelativeToProjectDirectory(_unconfiguredProject, unevaluatedPropertyValue, out var relativePath))
            //{
            
            //}

            //await defaultProperties.SetPropertyValueAsync("PackageIconUrl")
            return CreatePackageIcon(relativePath, packagePath);
            //return null;
        }

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            //if (string.IsNullOrEmpty(evaluatedPropertyValue))
            //{
            //    return Task.FromResult(NoneValue);
            //}

            //if (PathHelper.TryMakeRelativeToProjectDirectory(_unconfiguredProject, evaluatedPropertyValue, out var relativePath))
            //{
            //    return Task.FromResult(relativePath);
            //}

            //return Task.FromResult(string.Empty);
            //return Task.FromResult(evaluatedPropertyValue);
            //return null;

            //foreach (var noneItem in await _sourceItemsProvider.GetItemsAsync(NoneItem))
            //{
            //    if (noneItem.EvaluatedInclude.EndsWith(evaluatedPropertyValue))
            //    {
            //        return noneItem.EvaluatedInclude;
            //    }
            //}

            foreach (IProjectItem noneItem in await _sourceItemsProvider.GetItemsAsync(NoneItem))
            {
                string? pack = await noneItem.Metadata.GetEvaluatedPropertyValueAsync(PackPropertyName);
                if (TrueValue.Equals(pack, StringComparison.OrdinalIgnoreCase))
                {
                    string? itemPackagePath = await noneItem.Metadata.GetEvaluatedPropertyValueAsync(PackagePathPropertyName);
                    string itemPackageIcon = CreatePackageIcon(noneItem.EvaluatedInclude, itemPackagePath);

                    // If this is true, this item is the None item related to the PackageIcon.
                    if (itemPackageIcon.Equals(evaluatedPropertyValue, StringComparison.Ordinal))
                    {
                        return noneItem.EvaluatedInclude;
                        //return itemPackageIcon;
                    }
                }
            }

            //string filename = Path.GetFileName(evaluatedPropertyValue);
            return evaluatedPropertyValue;
        }
    }
}
