// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UI.InfoBarService;

/// <summary>
/// Implementation of <see cref="IInfoBarService"/> that pushes messages to the info bar attached to Visual Studio's main window.
/// </summary>
[Export(typeof(IInfoBarService))]
internal partial class VsInfoBarService : IInfoBarService
{
    private readonly IProjectThreadingService _threadingService;
    private readonly IVsShellServices _vsShellServices;
    private readonly IVsUIService<SVsShell, IVsShell> _vsShell;
    private readonly IVsUIService<SVsInfoBarUIFactory, IVsInfoBarUIFactory> _vsInfoBarFactory;
    private readonly List<InfoBarEntry> _entries = new();

    [ImportingConstructor]
    public VsInfoBarService(
        IProjectThreadingService threadingService,
        IVsShellServices vsShellServices,
        IVsUIService<SVsShell, IVsShell> vsShell,
        IVsUIService<SVsInfoBarUIFactory, IVsInfoBarUIFactory> vsInfoBarFactory)
    {
        _threadingService = threadingService;
        _vsShellServices = vsShellServices;
        _vsShell = vsShell;
        _vsInfoBarFactory = vsInfoBarFactory;
    }

    public async Task ShowInfoBarAsync(string message, ImageMoniker image, CancellationToken cancellationToken, params InfoBarUI[] items)
    {
        Requires.NotNullOrEmpty(message, nameof(message));
        Requires.NotNull(items, nameof(items));

        if (!await _vsShellServices.IsCommandLineModeAsync(cancellationToken))
        {
            await _threadingService.SwitchToUIThread(cancellationToken);

            IVsInfoBarHost? host = FindMainWindowInfoBarHost();
            if (host is null)
            {
                return;
            }

            // We want to avoid posting the same message over and over again, so we remove any existing info bar
            // with the same message, and add the new one which will cause it to float to the bottom of the host.
            RemoveInfoBar(message);
            AddInfoBar(host, message, image, items);
        }
    }

    private IVsInfoBarHost? FindMainWindowInfoBarHost()
    {
        if (_vsShell.Value is null)
        {
            return null;
        }

        if (ErrorHandler.Failed(_vsShell.Value.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out object mainWindowInfoBarHost)))
        {
            return null;
        }

        return mainWindowInfoBarHost as IVsInfoBarHost;
    }

    private IVsInfoBarUIElement? CreateInfoBarUIElement(string message, ImageMoniker image, InfoBarUI[] items)
    {
        if (_vsInfoBarFactory.Value is null)
        {
            return null;
        }

        var actionItems = new List<IVsInfoBarActionItem>(items.Length);

        foreach (InfoBarUI item in items)
        {
            switch (item.Kind)
            {
                default:
                case InfoBarUIKind.Button:
                    Assumes.True(item.Kind == InfoBarUIKind.Button);
                    actionItems.Add(new InfoBarButton(item.Title));
                    break;
                case InfoBarUIKind.Hyperlink:
                    actionItems.Add(new InfoBarHyperlink(item.Title));
                    break;
            }
        }

        var infoBarModel = new InfoBarModel(
            message,
            actionItems.ToArray(),
            image,
            isCloseButtonVisible: true);

        return _vsInfoBarFactory.Value.CreateInfoBar(infoBarModel);
    }

    private void AddInfoBar(IVsInfoBarHost host, string message, ImageMoniker image, InfoBarUI[] items)
    {
        IVsInfoBarUIElement? element = CreateInfoBarUIElement(message, image, items);
        if (element is not null)
        {
            var entry = new InfoBarEntry(message, element, items, OnClosed);
            _entries.Add(entry);
            host.AddInfoBar(element);
        }
    }

    private void RemoveInfoBar(string message)
    {
        // Assumption is that message is "good" enough to uniquely identify problems
        InfoBarEntry entry = _entries.Find(e => e.Message == message);
        entry?.Close();
    }

    private void OnClosed(InfoBarEntry entry)
    {
        Requires.NotNull(entry, nameof(entry));

        _threadingService.VerifyOnUIThread();

        _entries.Remove(entry);
    }
}
