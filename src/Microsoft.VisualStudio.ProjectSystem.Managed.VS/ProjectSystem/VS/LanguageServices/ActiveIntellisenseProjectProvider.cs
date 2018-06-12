// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    [Export(typeof(IActiveIntellisenseProjectProvider))]
    [ExportProjectNodeComService(typeof(IVsContainedLanguageProjectNameProvider))]
    internal class ActiveIntellisenseProjectProvider : IActiveIntellisenseProjectProvider, IVsContainedLanguageProjectNameProvider
    {
        [ImportingConstructor]
        public ActiveIntellisenseProjectProvider(UnconfiguredProject project)
        {
        }

        public string ActiveIntellisenseProjectContext
        {
            get;
            set;
        }

        public int GetProjectName(uint itemid, out string pbstrProjectName)
        {
            pbstrProjectName = ActiveIntellisenseProjectContext;
            return HResult.OK;
        }
    }
}
