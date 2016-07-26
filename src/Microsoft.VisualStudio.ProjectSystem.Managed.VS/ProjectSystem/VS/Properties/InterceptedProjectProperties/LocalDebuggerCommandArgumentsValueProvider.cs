// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    [ExportInterceptingPropertyValueProvider("LocalDebuggerCommandArguments", ExportInterceptingPropertyValueProviderFile.UserFile)]
    internal sealed class LocalDebuggerCommandArgumentsValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly Lazy<ProjectProperties> _projectProperties;
        private readonly Lazy<IOutputGroupsService, IAppliesToMetadataView> _outputGroups;
        private readonly ConfiguredProject _configuredProject;

        [ImportingConstructor]
        public LocalDebuggerCommandArgumentsValueProvider(Lazy<ProjectProperties> projectProperties, Lazy<IOutputGroupsService, IAppliesToMetadataView> outputGroups, ConfiguredProject configuredProject)
        {
            _projectProperties = projectProperties;
            _outputGroups = outputGroups;
            _configuredProject = configuredProject;
        }

        private Lazy<IOutputGroupsService> OutputGroups {
            get {
                return _outputGroups != null && _outputGroups.AppliesTo(_configuredProject.Capabilities) ? _outputGroups : null;
            }
        }

        public async override Task<string> OnGetEvaluatedPropertyValueAsync(string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            // Rather than going through defaultProperties for this, we want to get the decorated and intercepted debugger properties, so we
            // can get the final debug command.
            var decoratedDebuggerProperties = await _projectProperties.Value.GetWindowsLocalDebuggerPropertiesAsync().ConfigureAwait(false);
            var debugCommand = await decoratedDebuggerProperties.LocalDebuggerCommand.GetEvaluatedValueAtEndAsync().ConfigureAwait(false);

            var commandArgs = evaluatedPropertyValue;

            if (debugCommand == LocalDebuggerCommandValueProvider.DotnetExe)
            {
                // Get the path of the executable, and plug it into "exec path original_args"
                var executable = await GetExecutablePath().ConfigureAwait(false);
                commandArgs = $"exec {executable} {commandArgs}";
            }

            return commandArgs;
        }

        private async Task<string> GetExecutablePath()
        {
            var generalProperties = await _projectProperties.Value.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(false);
            var command = await generalProperties.TargetPath.GetEvaluatedValueAtEndAsync().ConfigureAwait(false);

            if (string.IsNullOrEmpty(command) && OutputGroups != null)
            {
                command = await OutputGroups.Value.GetKeyOutputAsync().ConfigureAwait(false);
            }

            return command;
        }
    }
}
