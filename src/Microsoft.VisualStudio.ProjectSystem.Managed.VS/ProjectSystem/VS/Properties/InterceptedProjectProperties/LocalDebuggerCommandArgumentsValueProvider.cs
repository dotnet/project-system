// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    [ExportInterceptingPropertyValueProvider("LocalDebuggerCommandArguments", ExportInterceptingPropertyValueProviderFile.UserFileWithXamlDefaults)]
    internal sealed class LocalDebuggerCommandArgumentsValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly Lazy<ProjectProperties> _projectProperties;
        private readonly Lazy<IOutputGroupsService, IAppliesToMetadataView> _outputGroups;
        private readonly ConfiguredProject _configuredProject;

        [ImportingConstructor]
        public LocalDebuggerCommandArgumentsValueProvider(Lazy<ProjectProperties> projectProperties,
                                                          [Import(AllowDefault = true)] Lazy<IOutputGroupsService, IAppliesToMetadataView> outputGroups,
                                                          ConfiguredProject configuredProject)
        {
            Requires.NotNull(projectProperties, nameof(projectProperties));
            Requires.NotNull(configuredProject, nameof(configuredProject));

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
            // We do the same check as in the debugger command, so empty string for the local debugger properties means we should perform
            // the interception.
            var debugCommand = await defaultProperties.GetEvaluatedPropertyValueAsync(WindowsLocalDebugger.LocalDebuggerCommandProperty).ConfigureAwait(false);
            var commandArgs = evaluatedPropertyValue;

            if (debugCommand == LocalDebuggerCommandValueProvider.DefaultCommand)
            {
                // Get the path of the executable, and plug it into "exec path original_args"
                var executable = await GetExecutablePath().ConfigureAwait(false);
                commandArgs = $@"exec ""{executable}"" {commandArgs}";
            }

            return commandArgs;
        }

        private async Task<string> GetExecutablePath()
        {
            var generalProperties = await _projectProperties.Value.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(false);
            var command = await generalProperties.TargetPath.GetEvaluatedValueAsync().ConfigureAwait(false);

            if (string.IsNullOrEmpty(command) && OutputGroups != null)
            {
                command = await OutputGroups.Value.GetKeyOutputAsync().ConfigureAwait(false);
            }

            // Because a .NET Core app produces an executable dll, and not an actual executable, we need to change the extension to be .dll
            var rawName = Path.GetFileNameWithoutExtension(command);
            var folder = new FileInfo(command).Directory.FullName;

            command = folder + Path.DirectorySeparatorChar + rawName + ".dll";

            return command;
        }
    }
}
