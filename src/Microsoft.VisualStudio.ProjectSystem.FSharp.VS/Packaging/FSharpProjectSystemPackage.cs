// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.Packaging
{
    [Guid(PackageGuid)]
    [PackageRegistration(AllowsBackgroundLoading = true, RegisterUsing = RegistrationMethod.CodeBase, UseManagedResourcesOnly = true)]
    internal class FSharpProjectSystemPackage : AsyncPackage
    {
        public const string PackageGuid = "a724c878-e8fd-4feb-b537-60baba7eda83";

        private IVsRegisterProjectSelector _projectSelectorService;
        private uint _projectSelectorCookie = VSConstants.VSCOOKIE_NIL;

        public FSharpProjectSystemPackage()
        {
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

#pragma warning disable RS0030 // Do not used banned APIs
            _projectSelectorService = this.GetService<IVsRegisterProjectSelector, SVsRegisterProjectTypes>();
#pragma warning restore RS0030 // Do not used banned APIs
            Guid selectorGuid = typeof(FSharpProjectSelector).GUID;
            _projectSelectorService.RegisterProjectSelector(ref selectorGuid, new FSharpProjectSelector(), out _projectSelectorCookie);

            await base.InitializeAsync(cancellationToken, progress);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _projectSelectorCookie != VSConstants.VSCOOKIE_NIL)
            {
                _projectSelectorService?.UnregisterProjectSelector(_projectSelectorCookie);
                _projectSelectorCookie = VSConstants.VSCOOKIE_NIL;
            }

            base.Dispose(disposing);
        }
    }
}
