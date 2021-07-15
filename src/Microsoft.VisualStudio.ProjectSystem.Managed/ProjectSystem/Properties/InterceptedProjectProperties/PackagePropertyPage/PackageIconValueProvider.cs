// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties.Package
{
    [ExportInterceptingPropertyValueProvider(PackageIconPropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class PackageIconValueProvider : InterceptingPropertyValueProviderBase
    {
        internal const string PackageIconPropertyName = "PackageIcon";
        //internal const string NoneValue = "(none)";
        //private readonly IProjectAccessor _projectAccessor;
        //private readonly IProjectItemProvider _sourceItemsProvider;

        //public PackageIconValueProvider([Import(AllowDefault = true)] IProjectAccessor projectAccessor)
        //{
        //    _projectAccessor = projectAccessor;
        //}

        //public PackageIconValueProvider([Import(ExportContractNames.ProjectItemProviders.SourceFiles)] IProjectItemProvider sourceItemsProvider)
        //{
        //    _sourceItemsProvider = sourceItemsProvider;
        //}

        public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IProjectItemProvider sourceItemsProvider, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            //string filename = PathHelper.TryMakeRelativeToProjectDirectory()
            //if (string.Equals(unevaluatedPropertyValue, NoneValue))
            //{
            //    await defaultProperties.DeletePropertyAsync(NeutralLanguagePropertyName);
            //    return null;
            //}
            //await _projectAccessor.EnterWriteLockAsync((pc, ct) => Task.CompletedTask);
            await Task.CompletedTask;

            return unevaluatedPropertyValue;
            //return null;
        }

        public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties, IProjectItemProvider sourceItemsProvider)
        {
            //if (string.IsNullOrEmpty(evaluatedPropertyValue))
            //{
            //    return Task.FromResult(NoneValue);
            //}

            return Task.FromResult(evaluatedPropertyValue);
            //return null;
        }
    }
}
