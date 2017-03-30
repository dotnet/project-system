// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop1;

// We register ourselves as a new CPS "project type"
[assembly: ProjectTypeRegistration(projectTypeGuid: FSharpProjectSystemPackage.ProjectTypeGuid, displayName: "#1", displayProjectFileExtensions: "#2", defaultProjectExtension: "fsproj", language: "FSharp", resourcePackageGuid: FSharpProjectSystemPackage.PackageGuid, Capabilities = ManagedProjectSystemPackage.DefaultCapabilities + "; " + ProjectCapability.FSharp, DisableAsynchronousProjectTreeLoad = true)]

namespace Microsoft.VisualStudio.Shell.Interop1
{
    [Guid("B042860A-5A69-4259-BC88-F1C79AE16C50")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsRegisterProjectSelector
    {
        void RegisterProjectSelector(ref Guid rguidProjType, IVsProjectSelector pProjectSelector, [ComAliasName("VsShell.VSCOOKIE")] out uint pdwCookie);
        void UnregisterProjectSelector([ComAliasName("VsShell.VSCOOKIE")] uint dwCookie);
    }

    [Guid("DFAD4C39-FCB2-4BDF-A389-2EA6DB28F062")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsProjectSelector
    {
        void GetProjectFactoryGuid(Guid guidProjectType, string pszFilename, out Guid guidProjectFactory);
    }

    //
    // Summary:
    //     Passed to System.IServiceProvider.GetService(System.Type) to return a reference
    //     to Microsoft.VisualStudio.Shell.Interop.IVsRegisterProjectTypes.
    [ComVisible(false)]
    [Guid("F08400BB-0960-47f4-9E12-591DBF370546")]
#pragma warning disable IDE1006 // Naming Styles
    public interface SVsRegisterProjectTypes
#pragma warning restore IDE1006 // Naming Styles
    {
    }
}

namespace Microsoft.VisualStudio.Packaging
{
    [Guid(PackageGuid)]
    [PackageRegistration(AllowsBackgroundLoading = true, RegisterUsing = RegistrationMethod.CodeBase, UseManagedResourcesOnly = true)]
    [DplOptOutRegistration(ProjectTypeGuid, true)]
    internal class FSharpProjectSystemPackage : AsyncPackage
    {
        public const string ProjectTypeGuid = "6EC3EE1D-3C4E-46DD-8F32-0CC8E7565705";
        public const string LegacyProjectTypeGuid = "F2A71F9B-5D33-465A-A702-920D77279786";
        public const string PackageGuid = "a724c878-e8fd-4feb-b537-60baba7eda83";
        private const string ProjectTypeGuidFormatted = "{" + ProjectTypeGuid + "}";

        private IVsRegisterProjectSelector _projectSelectorRegistrar;
        private uint _projectSelectorCookie;

        public FSharpProjectSystemPackage()
        {
        }

        protected override System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            _projectSelectorRegistrar = this.GetService<IVsRegisterProjectSelector, SVsRegisterProjectTypes>();
            var selectorGuid = typeof(FSharpProjectSelector).GUID;
            _projectSelectorRegistrar.RegisterProjectSelector(ref selectorGuid, new FSharpProjectSelector(), out _projectSelectorCookie);

            return base.InitializeAsync(cancellationToken, progress);
        }

        protected override void Dispose(bool disposing)
        {
            _projectSelectorRegistrar.UnregisterProjectSelector(_projectSelectorCookie);
            base.Dispose(disposing);
        }
    }
}
