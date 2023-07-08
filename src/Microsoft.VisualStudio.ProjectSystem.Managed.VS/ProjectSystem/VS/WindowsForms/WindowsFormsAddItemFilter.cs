// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.WindowsForms
{
    /// <summary>
    /// A filter for the Add New Item dialog that filters out Windows Forms items from non-Windows Forms projects.
    /// </summary>
    [ExportProjectNodeComService(typeof(IVsFilterAddProjectItemDlg))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class WindowsFormsAddItemFilter : IVsFilterAddProjectItemDlg, IDisposable
    {
        private UnconfiguredProject? _project;

        [ImportingConstructor]
        public WindowsFormsAddItemFilter(UnconfiguredProject project)
        {
            _project = project;
        }

        public int FilterTreeItemByLocalizedName(ref Guid rguidProjectItemTemplates, string pszLocalizedName, out int pfFilter)
        {
            pfFilter = 0;
            return HResult.NotImplemented;
        }

        public int FilterTreeItemByTemplateDir(ref Guid rguidProjectItemTemplates, string pszTemplateDir, out int pfFilter)
        {
            pfFilter = 0;

            var project = _project;
            if (project is null)
            {
                return HResult.Unexpected;
            }

            // Most of the templates for Windows Forms items are filtered by capabilities in the .vstemplate but there are a couple
            // that use an older .vsz tmeplate format that doesn't support capabilities, so we filter them all out here.
            if (pszTemplateDir.EndsWith("\\Windows Forms", StringComparisons.Paths) && !project.Capabilities.AppliesTo(ProjectCapability.WindowsForms))
            {
                pfFilter = 1;
            }
            return HResult.OK;
        }

        public int FilterListItemByLocalizedName(ref Guid rguidProjectItemTemplates, string pszLocalizedName, out int pfFilter)
        {
            pfFilter = 0;
            return HResult.NotImplemented;
        }

        public int FilterListItemByTemplateFile(ref Guid rguidProjectItemTemplates, string pszTemplateFile, out int pfFilter)
        {
            pfFilter = 0;

            // These item templates are an older style that can't be filtered by capabilities, and use a Wizard that is broken for .NET Core
            if (pszTemplateFile.EndsWith("\\CSControlInheritanceWizard.vsz", StringComparisons.Paths) ||
                pszTemplateFile.EndsWith("\\CSFormInheritanceWizard.vsz", StringComparisons.Paths) ||
                pszTemplateFile.EndsWith("\\InheritedControl.vsz", StringComparisons.Paths) ||
                pszTemplateFile.EndsWith("\\InheritedForm.vsz", StringComparisons.Paths))
            {
                pfFilter = 1;
            }
            return HResult.OK;
        }

        public void Dispose()
        {
            // Important for ProjectNodeComServices to null out fields to reduce the amount 
            // of data we leak when extensions incorrectly holds onto the IVsHierarchy.
            _project = null;
        }
    }
}
