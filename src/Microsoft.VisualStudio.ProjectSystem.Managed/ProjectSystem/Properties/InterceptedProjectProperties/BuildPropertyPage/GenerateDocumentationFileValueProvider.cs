// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportInterceptingPropertyValueProvider("GenerateDocumentationFile", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class GenerateDocumentationFileValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly ITemporaryPropertyStorage _temporaryPropertyStorage;

        private const string DocumentationFileMSBuildProperty = "DocumentationFile";
        private static readonly string[] s_msBuildPropertyNames = { DocumentationFileMSBuildProperty };
        
        [ImportingConstructor]
        public GenerateDocumentationFileValueProvider(ITemporaryPropertyStorage temporaryPropertyStorage)
        {
            _temporaryPropertyStorage = temporaryPropertyStorage;
        }

        public override Task<bool> IsValueDefinedInContextAsync(string propertyName, IProjectProperties defaultProperties)
        {
            return IsValueDefinedInContextMSBuildPropertiesAsync(defaultProperties, s_msBuildPropertyNames);
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
                    await defaultProperties.RestoreValueIfNotCurrentlySetAsync(DocumentationFileMSBuildProperty, _temporaryPropertyStorage, dimensionalConditions);
                }
            }

            return null;
        }
    }
}
