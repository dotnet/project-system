// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    /// OutputType acts as a converter for the MSBuild OutputType value expressed as <see cref="VSLangProj.prjOutputType"/>.
    [ExportInterceptingPropertyValueProvider("OutputType", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class OutputTypeValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly ProjectProperties _properties;

        private readonly Dictionary<string, string> _getOutputTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"WinExe",          "0" },
            {"Exe",             "1" },
            {"Library",         "2" },
            {"AppContainerExe", "1" },
            {"WinMDObj",        "2" },
        };

        private readonly Dictionary<string, string> _setOutputTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"0", "WinExe" },
            {"1", "Exe" },
            {"2", "Library" },
        };

        [ImportingConstructor]
        public OutputTypeValueProvider(ProjectProperties properties)
        {
            _properties = properties;
        }

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            var configuration = await _properties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(false);
            var value = await configuration.OutputType.GetEvaluatedValueAtEndAsync().ConfigureAwait(false);
            return _getOutputTypeMap[value];
        }

        public override async Task<string> OnSetPropertyValueAsync(string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string> dimensionalConditions = null)
        {
            var value = _setOutputTypeMap[unevaluatedPropertyValue];
            var configuration = await _properties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(false);
            await configuration.OutputType.SetValueAsync(value).ConfigureAwait(false);

            // The value is set so there is no need to set the value again.
            return null;
        }
    }
}

