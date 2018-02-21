// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

using EnvDTE;

using EnvDTE80;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(IDteServices))]
    internal class DteServices : IDteServices
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IUnconfiguredProjectVsServices _projectVsServices;

        [ImportingConstructor]
        public DteServices([Import(typeof(SVsServiceProvider))]IServiceProvider serviceProvider, IUnconfiguredProjectVsServices projectVsServices)
        {
            _serviceProvider = serviceProvider;
            _projectVsServices = projectVsServices;
        }

        public DTE2 Dte
        {
            get
            {
                _projectVsServices.ThreadingService.VerifyOnUIThread();

                return _serviceProvider.GetService<DTE2, SDTE>();
            }
        }

        public Solution2 Solution
        {
            get { return (Solution2)Dte.Solution; }
        }

        public Project Project
        {
            get
            {
                _projectVsServices.ThreadingService.VerifyOnUIThread();

                return _projectVsServices.VsHierarchy.GetProperty<Project>(VsHierarchyPropID.ExtObject);
            }
        }
    }
}
