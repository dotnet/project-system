// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.WindowsForms
{
    /// <summary>
    /// A filter for the Add New Item dialog that filters out Windows Forms items from non-Windows Forms projects.
    /// </summary>
    [ExportProjectNodeComService(typeof(IVsFilterAddProjectItemDlg))]
    [AppliesTo("!" + ProjectCapability.WindowsForms)]
    internal class WindowsFormsAddItemFilter : IVsFilterAddProjectItemDlg
    {
        public int FilterTreeItemByLocalizedName(ref Guid rguidProjectItemTemplates, string pszLocalizedName, out int pfFilter)
        {
            pfFilter = 0;
            return HResult.NotImplemented;
        }

        public int FilterTreeItemByTemplateDir(ref Guid rguidProjectItemTemplates, string pszTemplateDir, out int pfFilter)
        {
            pfFilter = 0;
            // Most of the templates for Windows Forms items are filtered by capabilities in the .vstemplate but there are a couple
            // that use an older .vsz tmeplate format that doesn't support capabilities, so we filter them out here.
            // The AppliesTo on this class ensures this code only runs for Windows Forms projects
            if (pszTemplateDir.EndsWith("\\Windows Forms", StringComparisons.Paths))
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
            return HResult.NotImplemented;
        }
    }
}
