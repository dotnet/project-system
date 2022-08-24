// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Shell.Interop;

#nullable disable

namespace Microsoft.VisualStudio.Shell;

/// <summary>
/// See IVSShellServices.
/// </summary>
[Export(typeof(IVsShellServices))]
[AppliesTo(ProjectCapabilities.AlwaysApplicable)]
internal class VSShellServices : OnceInitializedOnceDisposed, IVsShellServices
{
    /// <summary>
    /// The service provider providing access to the VS services.
    /// </summary>
    [Import]
    private IVsUIService<SVsShell, IVsShell> ServiceProvider { get; set; }

    public bool IsInServerMode { get; private set; }

    /// <summary>
    /// The SVsShell service.
    /// </summary>
    private IVsShell vsShell;

    /// <summary>
    /// Initializes a new instance of the <see cref="VSShellServices"/> class.
    /// </summary>
    public VSShellServices()
        : base(synchronousDisposal: true) // disposal requires UI thread unadvise, so async defeats that.
    {
    }

    protected override void Initialize()
    {
        vsShell = ServiceProvider.Value;

        if (ErrorHandler.Succeeded(vsShell.GetProperty((int)__VSSPROPID.VSSPROPID_IsInCommandLineMode, out object result)) && (bool)result)
        {
            IsInServerMode = CheckIsInServerMode();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool CheckIsInServerMode()
    {
        if (ErrorHandler.Succeeded(vsShell.GetProperty((int)__VSSPROPID11.VSSPROPID_ShellMode, out object value))
            && value is int shellMode
            && shellMode == (int)__VSShellMode.VSSM_Server)
            return true;

        return false;
    }

    protected override void Dispose(bool disposing)
    {
    }
}
