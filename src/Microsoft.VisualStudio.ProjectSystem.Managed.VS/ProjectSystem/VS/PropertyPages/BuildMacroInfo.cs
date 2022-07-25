// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    /// <summary>
    /// Implements the <see cref="IVsBuildMacroInfo"/> interface to be consumed by project properties.
    /// </summary>
    [ExportProjectNodeComService(typeof(IVsBuildMacroInfo))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class BuildMacroInfo : IVsBuildMacroInfo, IDisposable
    {
        private IProjectThreadingService? _threadingService;
        private IActiveConfiguredValue<ConfiguredProject>? _configuredProject;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildMacroInfo"/> class.
        /// </summary>
        /// <param name="configuredProject">Project being evaluated.</param>
        /// <param name="threadingService">Project threading service.</param>
        [ImportingConstructor]
        public BuildMacroInfo(
            IActiveConfiguredValue<ConfiguredProject> configuredProject,
            IProjectThreadingService threadingService)
        {
            _threadingService = threadingService;
            _configuredProject = configuredProject;
        }

        /// <summary>
        /// Retrieves the value or body of a macro based on the macro's name.
        /// </summary>
        /// <param name="bstrBuildMacroName">String containing the name of the macro.</param>
        /// <param name="pbstrBuildMacroValue">String containing the value or body of the macro.</param>
        /// <returns>If the method succeeds, it returns S_OK. If it fails, it returns an error code.</returns>
        public int GetBuildMacroValue(string bstrBuildMacroName, out string? pbstrBuildMacroValue)
        {
            if (_configuredProject is null)
            {
                pbstrBuildMacroValue = null;
                return HResult.Unexpected;
            }

            Assumes.Present(_configuredProject.Value.Services.ProjectPropertiesProvider);

            pbstrBuildMacroValue = null;
            ProjectSystem.Properties.IProjectProperties commonProperties = _configuredProject.Value.Services.ProjectPropertiesProvider.GetCommonProperties();
            pbstrBuildMacroValue = _threadingService?.ExecuteSynchronously(() => commonProperties.GetEvaluatedPropertyValueAsync(bstrBuildMacroName));

            if (string.IsNullOrEmpty(pbstrBuildMacroValue))
            {
                pbstrBuildMacroValue = string.Empty;
                return HResult.Fail;
            }
            else
            {
                return HResult.OK;
            }
        }

        public void Dispose()
        {
            // Important for ProjectNodeComServices to null out fields to reduce the amount 
            // of data we leak when extensions incorrectly holds onto the IVsHierarchy.
            _threadingService = null;
            _configuredProject = null;
        }
    }
}
