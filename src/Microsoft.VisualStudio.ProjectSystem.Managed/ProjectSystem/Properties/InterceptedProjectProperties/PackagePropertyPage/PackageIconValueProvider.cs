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
        private const string NoneItem = "None";

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
            string filename = Path.GetFileName(unevaluatedPropertyValue);

            // TODO: Need to use evaluated value of PackagePath... need to check this.
            // TODO: Need to calculate the PackageIcon based on PackagePath and file name... for matching.

            //foreach(var noneItem in await _sourceItemsProvider.GetItemsAsync(NoneItem)

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




            //if(PathHelper.TryMakeRelativeToProjectDirectory(_unconfiguredProject, unevaluatedPropertyValue, out var relativePath))
            //{
            await _sourceItemsProvider.AddAsync(NoneItem, relativePath, new Dictionary<string, string> { { "Pack", "True" }, { "PackagePath", string.Empty } });
            //}

            //await defaultProperties.SetPropertyValueAsync("PackageIconUrl")
            return filename;
            //return null;
        }

        public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            //if (string.IsNullOrEmpty(evaluatedPropertyValue))
            //{
            //    return Task.FromResult(NoneValue);
            //}

            if (PathHelper.TryMakeRelativeToProjectDirectory(_unconfiguredProject, evaluatedPropertyValue, out var relativePath))
            {
                return Task.FromResult(relativePath);
            }

            return Task.FromResult(string.Empty);
            //return Task.FromResult(evaluatedPropertyValue);
            //return null;
        }
    }
}
