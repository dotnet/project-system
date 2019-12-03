// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    /// This service creates a file from a given file template.
    /// </summary>
    [Export(typeof(ICreateFileFromTemplateService))]
    internal class CreateFileFromTemplateService : ICreateFileFromTemplateService
    {
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly IVsUIService<DTE2> _dte;
        private readonly ProjectProperties _properties;

        [ImportingConstructor]
        public CreateFileFromTemplateService(IUnconfiguredProjectVsServices projectVsServices, IVsUIService<SDTE, DTE2> dte, ProjectProperties properties)
        {
            _projectVsServices = projectVsServices;
            _dte = dte;
            _properties = properties;
        }

        /// <summary>
        /// Create a file with the given template file and add it to the parent node.
        /// </summary>
        /// <param name="templateFile">The name of the template zip file.</param>
        /// <param name="path">The path to the file to be created.</param>
        /// <returns>true if file is added successfully.</returns>
        public async Task<bool> CreateFileAsync(string templateFile, string path)
        {
            Requires.NotNull(templateFile, nameof(templateFile));
            Requires.NotNullOrEmpty(path, nameof(path));

            string directoryName = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);

            string? templateLanguage = await GetTemplateLanguageAsync();
            if (string.IsNullOrEmpty(templateLanguage))
                return false;

            await _projectVsServices.ThreadingService.SwitchToUIThread();

            DTE2? dte = _dte.Value;
            Assumes.Present(dte);

            string templateFilePath = ((Solution2)dte.Solution).GetProjectItemTemplate(templateFile, templateLanguage);

            if (templateFilePath != null)
            {
                HierarchyId parentId = _projectVsServices.VsProject.GetHierarchyId(directoryName);
                var result = new VSADDRESULT[1];
                string[] files = new string[] { templateFilePath };
                _projectVsServices.VsProject.AddItemWithSpecific(parentId, VSADDITEMOPERATION.VSADDITEMOP_RUNWIZARD, fileName, (uint)files.Length, files, IntPtr.Zero, 0, Guid.Empty, null, Guid.Empty, result);

                if (result[0] == VSADDRESULT.ADDRESULT_Success)
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<string?> GetTemplateLanguageAsync()
        {
            ConfigurationGeneral general = await _properties.GetConfigurationGeneralPropertiesAsync();

            return (string?)await general.TemplateLanguage.GetValueAsync();
        }
    }
}
