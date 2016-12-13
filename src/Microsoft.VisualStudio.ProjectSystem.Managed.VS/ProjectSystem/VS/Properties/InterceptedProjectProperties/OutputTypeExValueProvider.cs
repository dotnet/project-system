// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    // OutputTypeEx acts as a converter for the OutputType value to VSLangProj110.prjOutputTypeEx.
    [ExportInterceptingPropertyValueProvider("OutputTypeEx", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class OutputTypeExValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly ProjectProperties _properties;

        [ImportingConstructor]
        public OutputTypeExValueProvider(ProjectProperties properties)
        {
            _properties = properties;
        }

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            var configuration = await _properties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(true);
            var rawValue = await configuration.OutputType.GetValueAsync().ConfigureAwait(true);

            string value = null, outputType = null;
            if (rawValue is string)
            {
                outputType = (string)rawValue;
            }
            else if (rawValue is IEnumValue)
            {
                outputType = ((IEnumValue)rawValue).Name;
            }

            if (outputType != null)
            {
                switch (outputType.ToLowerInvariant())
                {
                    case "winexe":
                        // prjOutputTypeEx_WinExe
                        value = "0";
                        break;
                    case "exe":
                        // prjOutputTypeEx_Exe
                        value = "1";
                        break;
                    case "library":
                        // prjOutputTypeEx_Library
                        value = "2";
                        break;
                    case "winmdobj":
                        // prjOutputTypeEx_WinMDObj
                        value = "3";
                        break;
                    case "appcontainerexe":
                        // prjOutputTypeEx_AppContainerExe
                        value = "4";
                        break;
                }
            }

            return await Task.FromResult<string>(value).ConfigureAwait(false);
        }

        public override async Task<string> OnSetPropertyValueAsync(string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string> dimensionalConditions = null)
        {
            var configuration = await _properties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(true);
            await configuration.OutputType.SetValueAsync(unevaluatedPropertyValue).ConfigureAwait(false);
            return unevaluatedPropertyValue;
        }
    }
}
