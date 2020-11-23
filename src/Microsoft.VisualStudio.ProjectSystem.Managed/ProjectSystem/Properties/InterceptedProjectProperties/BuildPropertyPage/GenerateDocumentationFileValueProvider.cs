// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportInterceptingPropertyValueProvider("GenerateDocumentationFile", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class GenerateDocumentationFileValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly ITemporaryPropertyStorage _temporaryPropertyStorage;

        private const string DocumentationFileMSBuildProperty = "DocumentationFile";

        [ImportingConstructor]
        public GenerateDocumentationFileValueProvider(ITemporaryPropertyStorage temporaryPropertyStorage)
        {
            _temporaryPropertyStorage = temporaryPropertyStorage;
        }

        public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            if (bool.TryParse(unevaluatedPropertyValue, out bool value))
            {
                if (!value)
                {
                    await defaultProperties.SaveValueIfCurrentlySetAsync(DocumentationFileMSBuildProperty, _temporaryPropertyStorage);
                    await defaultProperties.DeletePropertyAsync(DocumentationFileMSBuildProperty);
                }
                else
                {
                    await defaultProperties.RestoreValueIfNotCurrentlySetAsync(DocumentationFileMSBuildProperty, _temporaryPropertyStorage);
                }
            }

            return null;
        }
    }
}
