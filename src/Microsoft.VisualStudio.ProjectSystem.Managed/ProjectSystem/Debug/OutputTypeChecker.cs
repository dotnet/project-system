// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    internal class OutputTypeChecker
    {
        private readonly ProjectProperties _properties;

        public OutputTypeChecker(ProjectProperties properties)
        {
            _properties = properties;
        }

        public Task<bool> IsLibraryAsync() => IsOutputTypeAsync(ConfigurationGeneral.OutputTypeValues.Library);

        public Task<bool> IsConsoleAsync() => IsOutputTypeAsync(ConfigurationGeneral.OutputTypeValues.Exe);

        public async Task<bool> IsOutputTypeAsync(string outputType)
        {
            IEnumValue? actualOutputType = await GetEvaluatedOutputTypeAsync();

            return actualOutputType is not null && StringComparers.PropertyLiteralValues.Equals(actualOutputType.Name, outputType);
        }

        public virtual async Task<IEnumValue?> GetEvaluatedOutputTypeAsync()
        {
            // Used by default Windows debugger to figure out whether to add an extra
            // pause to end of window when CTRL+F5'ing a console application
            ConfigurationGeneral configuration = await _properties.GetConfigurationGeneralPropertiesAsync();

            var actualOutputType = (IEnumValue?)await configuration.OutputType.GetValueAsync();

            return actualOutputType;
        }
    }
}
