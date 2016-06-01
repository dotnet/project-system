// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(ICreateFileFromTemplateService))]
    internal class CreateFileFromTemplateService : ICreateFileFromTemplateService
    {
        [Import]
        private IUnconfiguredProjectVsServices ProjectVsServices { get; set; }

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

        public async Task<bool> CreateFileAsync(string templateFile, IProjectTree parentNode, string specialFileName)
        {
            Project project = ProjectVsServices.Hierarchy.GetProperty<Project>(Shell.VsHierarchyPropID.ExtObject, null);
            var solution = project.DTE.Solution as Solution2;

            await ProjectVsServices.ThreadingService.SwitchToUIThread();

            string templateFilePath = solution.GetProjectItemTemplate(templateFile, GetTemplateLanguage(project));

            // Create file.
            if (templateFilePath != null)
            {
                var parentId = parentNode.IsRoot() ? (uint)VSConstants.VSITEMID.Root : (uint)parentNode.Identity;
                var result = new VSADDRESULT[1];
                ProjectVsServices.Project.AddItemWithSpecific(parentId, VSADDITEMOPERATION.VSADDITEMOP_RUNWIZARD, specialFileName, 0, new string[] { templateFilePath }, IntPtr.Zero, 0, Guid.Empty, null, Guid.Empty, result);

                if (result[0] == VSADDRESULT.ADDRESULT_Success)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
