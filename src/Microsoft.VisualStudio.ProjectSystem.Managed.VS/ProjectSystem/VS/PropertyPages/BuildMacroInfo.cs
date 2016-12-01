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
        /// Project threading service.
        /// </summary>
        private readonly IProjectThreadingService _threadingService;

        /// <summary>
        /// Project components for the configuration being evaluated.
        /// </summary>
        private UnconfiguredProject _unconfiguredProject;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildMacroInfo"/> class.
        /// </summary>
        /// <param name="unconfiguredProject">Project being evaluated.</param>
        /// <param name="threadingService">Project threading service.</param>
        [ImportingConstructor]
        public BuildMacroInfo(
            UnconfiguredProject unconfiguredProject,
            IProjectThreadingService threadingService)
        {
            _unconfiguredProject = unconfiguredProject;
            _threadingService = threadingService;
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
            var configuredProject = _threadingService.ExecuteSynchronously(_unconfiguredProject.GetSuggestedConfiguredProjectAsync);
            var commonProperties = configuredProject.Services.ProjectPropertiesProvider.GetCommonProperties();
            pbstrBuildMacroValue = _threadingService.ExecuteSynchronously<string>(() => commonProperties.GetEvaluatedPropertyValueAsync(bstrBuildMacroName));

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
