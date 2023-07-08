// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ProjectSystem.WPF;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Design.Serialization;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.WPF;

[Export(typeof(IApplicationXamlFileAccessor))]
[AppliesTo(ProjectCapability.DotNet)]
internal class ApplicationXamlFileAccessor : IApplicationXamlFileAccessor
{
    private readonly UnconfiguredProject _project;
    private readonly IServiceProvider _serviceProvider;
    private readonly IVsUIService<IVsRunningDocumentTable> _runningDocumentTable;
    private readonly IProjectThreadingService _threadingService;

    private DocData? _applicationXamlDocdata;
    private AppDotXamlDocument? _applicationDotXamlDocument;

    [ImportingConstructor]
    public ApplicationXamlFileAccessor(
        UnconfiguredProject project,
#pragma warning disable RS0030 // Do not used banned APIs
        [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
#pragma warning restore RS0030 // Do not used banned APIs
        IVsUIService<IVsRunningDocumentTable> runningDocumentTable,
        IProjectThreadingService threadingService)
    {
        _project = project;
        _serviceProvider = serviceProvider;
        _runningDocumentTable = runningDocumentTable;
        _threadingService = threadingService;
    }

    public async Task<string?> GetStartupUriAsync()
    {
        await _threadingService.SwitchToUIThread();

        AppDotXamlDocument? xamlDocument = await TryGetApplicationXamlFileAsync(create: false);

        return xamlDocument?.GetStartupUri();
    }

    public async Task SetStartupUriAsync(string startupUri)
    {
        await _threadingService.SwitchToUIThread();

        AppDotXamlDocument? xamlDocument = await TryGetApplicationXamlFileAsync(create: true);

        if (xamlDocument is not null)
        {
            xamlDocument.SetStartupUri(startupUri);
            SaveDocDataIfOnlyEditor();
        }
    }

    public async Task<string?> GetShutdownModeAsync()
    {
        await _threadingService.SwitchToUIThread();

        AppDotXamlDocument? xamlDocument = await TryGetApplicationXamlFileAsync(create: false);

        return xamlDocument?.GetShutdownMode();
    }

    public async Task SetShutdownModeAsync(string shutdownMode)
    {
        await _threadingService.SwitchToUIThread();

        AppDotXamlDocument? xamlDocument = await TryGetApplicationXamlFileAsync(create: true);

        if (xamlDocument is not null)
        {
            xamlDocument.SetShutdownMode(shutdownMode);
            SaveDocDataIfOnlyEditor();
        }
    }

    private async Task<AppDotXamlDocument?> TryGetApplicationXamlFileAsync(bool create)
    {
        if (_applicationDotXamlDocument is null)
        {
            DocData? docData = await GetDocDataAsync(create);

            if (docData is not null)
            {
                if (docData.Buffer is IVsTextLines vsTextLines)
                {
                    _applicationDotXamlDocument = new AppDotXamlDocument(vsTextLines);
                }
            }
        }

        return _applicationDotXamlDocument;

        async Task<DocData?> GetDocDataAsync(bool create)
        {
            if (_applicationXamlDocdata is null)
            {
                string? filePath = await GetFilePathAsync(create);
                if (filePath is not null)
                {
                    _applicationXamlDocdata = new DocData(_serviceProvider, filePath);
                }
            }

            return _applicationXamlDocdata;
        }

        async Task<string?> GetFilePathAsync(bool create)
        {
            SpecialFileFlags flags = SpecialFileFlags.FullPath | SpecialFileFlags.CheckoutIfExists;
            if (create)
            {
                flags |= SpecialFileFlags.CreateIfNotExist;
            }

            return await _project.GetSpecialFilePathAsync(SpecialFiles.AppXaml, flags);
        }
    }

    private void SaveDocDataIfOnlyEditor()
    {
        if (_applicationXamlDocdata is null)
        {
            return;
        }

        IntPtr unknownDocData = IntPtr.Zero;
        uint cookie;

        try
        {
            ErrorHandler.ThrowOnFailure(_runningDocumentTable.Value.FindAndLockDocument(
                (uint)_VSRDTFLAGS.RDT_NoLock,
                _applicationXamlDocdata.Name,
                out _,
                out _,
                out unknownDocData,
                out cookie));
        }
        finally
        {
            if (!unknownDocData.Equals(IntPtr.Zero))
            {
                Marshal.Release(unknownDocData);
            }
        }

        IVsHierarchy hierarchy;
        uint itemId;
        uint editLocks;

        try
        {
            ErrorHandler.ThrowOnFailure(_runningDocumentTable.Value.GetDocumentInfo(
                cookie,
                out _,
                out _,
                out editLocks,
                out _,
                out hierarchy,
                out itemId,
                out unknownDocData));
        }
        finally
        {
            if (!unknownDocData.Equals(IntPtr.Zero))
            {
                Marshal.Release(unknownDocData);
            }
        }

        if (editLocks == 1)
        {
            // We're the only ones with it open; save the document.
            ErrorHandler.ThrowOnFailure(_runningDocumentTable.Value.SaveDocuments((uint)__VSRDTSAVEOPTIONS.RDTSAVEOPT_SaveIfDirty, hierarchy, itemId, cookie));
        }
    }
}
