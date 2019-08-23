// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal class VsStartupProjectsListService : IVsStartupProjectsListService
    {
        public Guid? ProjectGuid
        {
            get;
            private set;
        }

        public void AddProject(ref Guid guidProject)
        {
            ProjectGuid = guidProject;
        }

        public void RemoveProject(ref Guid guidProject)
        {
            if (guidProject == ProjectGuid)
                ProjectGuid = null;
        }
    }
}
