// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;

using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Task = System.Threading.Tasks.Task;

// Register ourselves as a CPS project type
[assembly: ProjectTypeRegistration(
    projectTypeGuid: FSharpProjectSystemPackage.ProjectTypeGuid,
    displayName: "#1",
    displayProjectFileExtensions: "#2",
    defaultProjectExtension: "fsproj",
    language: "FSharp",
    resourcePackageGuid: FSharpProjectSystemPackage.PackageGuid,
    Capabilities = ManagedProjectSystemPackage.DefaultCapabilities + "; " + ProjectCapability.FSharp + "; " + ProjectCapability.SortByDisplayOrder,
    DisableAsynchronousProjectTreeLoad = true)]

namespace Microsoft.VisualStudio.Packaging
{
    [Guid(PackageGuid)]
    [PackageRegistration(AllowsBackgroundLoading = true, RegisterUsing = RegistrationMethod.CodeBase, UseManagedResourcesOnly = true)]
    internal class FSharpProjectSystemPackage : AsyncPackage
    {
        public const string ProjectTypeGuid = "6EC3EE1D-3C4E-46DD-8F32-0CC8E7565705";
        public const string LegacyProjectTypeGuid = "F2A71F9B-5D33-465A-A702-920D77279786";
        public const string PackageGuid = "a724c878-e8fd-4feb-b537-60baba7eda83";

        private IVsRegisterProjectSelector _projectSelectorService;
        private uint _projectSelectorCookie = VSConstants.VSCOOKIE_NIL;

        public FSharpProjectSystemPackage()
        {
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            _projectSelectorService = this.GetService<IVsRegisterProjectSelector, SVsRegisterProjectTypes>();
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
