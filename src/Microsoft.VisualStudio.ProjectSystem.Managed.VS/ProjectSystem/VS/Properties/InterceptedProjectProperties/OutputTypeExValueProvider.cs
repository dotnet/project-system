// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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

        private readonly Dictionary<string, string> _getOutputTypeExMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"WinExe",          "0" },
            {"Exe",             "1" },
            {"Library",         "2" },
            {"AppContainerExe", "3" },
            {"WinMDObj",        "4" },
        };

        private readonly Dictionary<string, string> _setOutputTypeExMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"0", "WinExe" },
            {"1", "Exe" },
            {"2", "Library" },
            {"3", "AppContainerExe" },
            {"4", "WinMDObj"},
        };

        [ImportingConstructor]
        public OutputTypeExValueProvider(ProjectProperties properties)
        {
            _properties = properties;
        }

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            var configuration = await _properties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(false);
            var value = await configuration.OutputType.GetEvaluatedValueAtEndAsync().ConfigureAwait(false);
            return _getOutputTypeExMap[value];
        }

        public override async Task<string> OnSetPropertyValueAsync(string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string> dimensionalConditions = null)
        {
            var value = _setOutputTypeExMap[unevaluatedPropertyValue];
            var configuration = await _properties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(false);
            await configuration.OutputType.SetValueAsync(value).ConfigureAwait(false);

            // We need to return null so we dont persist a value for the Msbuild property 'OutputTypeEx'
            return null;
        }
    }
}
