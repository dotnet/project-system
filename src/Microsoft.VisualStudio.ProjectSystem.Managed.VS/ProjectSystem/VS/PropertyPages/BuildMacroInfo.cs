// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    /// <summary>
    /// Implements the <see cref="IVsBuildMacroInfo"/> interface to be consumed by project properties.
    /// </summary>
    [Export(ExportContractNames.VsTypes.ProjectNodeComExtension)]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    [ComServiceIid(typeof(IVsBuildMacroInfo))]
    internal class BuildMacroInfo : IVsBuildMacroInfo
    {
        /// <summary>
        /// Provides access to common Visual Studio project services.
        /// </summary>
        private IUnconfiguredProjectVsServices _projectVsServices;

        /// <summary>
        /// Project components for the configuration being evaluated.
        /// </summary>
        private UnconfiguredProject _unconfiguredProject;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildMacroInfo"/> class.
        /// </summary>
        /// <param name="unconfiguredProject">Project being evaluated.</param>
        /// <param name="projectVsServices">Visual Studio project services.</param>
        [ImportingConstructor]
        public BuildMacroInfo(
            UnconfiguredProject unconfiguredProject,
            IUnconfiguredProjectVsServices projectVsServices
            )
        {
            _unconfiguredProject = unconfiguredProject;
            _projectVsServices = projectVsServices;
        }

        #region IVsBuildMacroInfo

        /// <summary>
        /// Retrieves the value or body of a macro based on the macro's name.
        /// </summary>
        /// <param name="bstrBuildMacroName">String containing the name of the macro.</param>
        /// <param name="pbstrBuildMacroValue">String containing the value or body of the macro.</param>
        /// <returns>If the method succeeds, it returns S_OK. If it fails, it returns an error code.</returns>
        public int GetBuildMacroValue(string bstrBuildMacroName, out string pbstrBuildMacroValue)
        {
            pbstrBuildMacroValue = null;
            var configuredProject = _projectVsServices.ThreadingService.ExecuteSynchronously(async delegate
            {
                return await _unconfiguredProject.GetSuggestedConfiguredProjectAsync().ConfigureAwait(false);
            });

            var commonProperties = configuredProject.Services.ProjectPropertiesProvider.GetCommonProperties();
            pbstrBuildMacroValue = _projectVsServices.ThreadingService.ExecuteSynchronously<string>(async delegate
            {
                return await commonProperties.GetEvaluatedPropertyValueAsync(bstrBuildMacroName).ConfigureAwait(false);
            });

            if (pbstrBuildMacroValue == null)
            {
                pbstrBuildMacroValue = string.Empty;
                return VSConstants.E_FAIL;
            }
            else
            {
                return VSConstants.S_OK;
            }
        }

        #endregion
    }
}
