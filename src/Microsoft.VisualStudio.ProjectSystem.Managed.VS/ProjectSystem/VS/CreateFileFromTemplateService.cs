// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
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

        [ImportingConstructor]
        public CreateFileFromTemplateService(IUnconfiguredProjectVsServices projectVsServices)
        {
            Requires.NotNull(projectVsServices, nameof(projectVsServices));

            _projectVsServices = projectVsServices;
        }

        /// <summary>
        /// Get the language string to pass to the VS APIs for getting a template.
        /// </summary>
        private string GetTemplateLanguage(Project project)
        {
            switch (project.CodeModel.Language)
            {
                case CodeModelLanguageConstants.vsCMLanguageCSharp:
                    return "CSharp";
                case CodeModelLanguageConstants.vsCMLanguageVB:
                    return "VisualBasic";
                default:
                    throw new NotSupportedException("Unrecognized language");
            }
        }

        /// <summary>
        /// Create a file with the given template file and add it to the parent node.
        /// </summary>
        /// <param name="templateFile">The name of the template zip file.</param>
        /// <param name="parentNode">The node to which the new file will be added.</param>
        /// <param name="fileName">The name of the file to be created.</param>
        /// <returns>true if file is added successfully.</returns>
        public async Task<bool> CreateFileAsync(string templateFile, IProjectTree parentNode, string fileName)
        {
            Requires.NotNull(templateFile, nameof(templateFile));
            Requires.NotNull(parentNode, nameof(parentNode));
            Requires.NotNull(fileName, nameof(fileName));

            await _projectVsServices.ThreadingService.SwitchToUIThread();

            Project project = _projectVsServices.VsHierarchy.GetProperty<Project>(Shell.VsHierarchyPropID.ExtObject, null);
            var solution = project.DTE.Solution as Solution2;

            string templateFilePath = solution.GetProjectItemTemplate(templateFile, GetTemplateLanguage(project));

            if (templateFilePath != null)
            {
                var parentId = parentNode.GetHierarchyId().Id;
                var result = new VSADDRESULT[1];
                _projectVsServices.VsProject.AddItemWithSpecific(parentId, VSADDITEMOPERATION.VSADDITEMOP_RUNWIZARD, fileName, 0, new string[] { templateFilePath }, IntPtr.Zero, 0, Guid.Empty, null, Guid.Empty, result);

                if (result[0] == VSADDRESULT.ADDRESULT_Success)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
