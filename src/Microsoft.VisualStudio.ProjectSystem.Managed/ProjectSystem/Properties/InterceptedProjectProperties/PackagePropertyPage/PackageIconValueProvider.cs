// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties.Package
{
    [ExportInterceptingPropertyValueProvider(PackageIconPropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class PackageIconValueProvider : InterceptingPropertyValueWithSourceItemsProviderBase
    {
        internal const string PackageIconPropertyName = "PackageIcon";

        private readonly UnconfiguredProject _unconfiguredProject;

        [ImportingConstructor]
        public PackageIconValueProvider(UnconfiguredProject unconfiguredProject)
        {
            _unconfiguredProject = unconfiguredProject;
        }

        public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IProjectItemProvider sourceItemsProvider, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            //string filename = PathHelper.TryMakeRelativeToProjectDirectory()
            //if (string.Equals(unevaluatedPropertyValue, NoneValue))
            //{
            //    await defaultProperties.DeletePropertyAsync(NeutralLanguagePropertyName);
            //    return null;
            //}
            //await _projectAccessor.EnterWriteLockAsync((pc, ct) => Task.CompletedTask);
            //await Task.CompletedTask;

            if(PathHelper.TryMakeRelativeToProjectDirectory(_unconfiguredProject, unevaluatedPropertyValue, out var relativePath))
            {
                await sourceItemsProvider.AddAsync("None", relativePath, new Dictionary<string, string> { { "Pack", "True" }, { "PackagePath", string.Empty } });
            }

            //await defaultProperties.SetPropertyValueAsync("PackageIconUrl")
            return Path.GetFileName(unevaluatedPropertyValue);
            //return null;
        }

        public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties, IProjectItemProvider sourceItemsProvider)
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
