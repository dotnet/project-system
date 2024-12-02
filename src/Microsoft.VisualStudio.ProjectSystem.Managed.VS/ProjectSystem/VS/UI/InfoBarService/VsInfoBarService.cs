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
[method: ImportingConstructor]
internal sealed partial class VsInfoBarService(
    IProjectThreadingService threadingService,
    IVsShellServices vsShellServices,
    IVsUIService<SVsShell, IVsShell> vsShell,
    IVsUIService<SVsInfoBarUIFactory, IVsInfoBarUIFactory> vsInfoBarFactory)
    : IInfoBarService
{
    private readonly List<InfoBarEntry> _entries = [];
    private IVsInfoBarHost? _host;

    public async Task ShowInfoBarAsync(string message, ImageMoniker image, CancellationToken cancellationToken, params ImmutableArray<InfoBarUI> items)
    {
        Requires.NotNullOrEmpty(message);

        if (await vsShellServices.IsCommandLineModeAsync(cancellationToken))
        {
            // We don't want to show info bars in command line mode, as there's no GUI.
            return;
        }

        await threadingService.SwitchToUIThread(cancellationToken);

        _host ??= FindMainWindowInfoBarHost();

        if (vsInfoBarFactory.Value is null || _host is null)
        {
            return;
        }

        // We want to avoid posting the same message over and over again, so we remove any existing info bar
        // with the same message, and add the new one which will cause it to float to the bottom of the host.
        //
        // Assumption is that message is enough to uniquely identify entries.
        _entries.Find(e => e.Message == message)?.Close();

        var infoBarModel = new InfoBarModel(
            message,
            items.Select(ToActionItem),
            image,
            isCloseButtonVisible: true);

        IVsInfoBarUIElement element = vsInfoBarFactory.Value.CreateInfoBar(infoBarModel);

        InfoBarEntry entry = new(message, element, items, OnClosed);

        _entries.Add(entry);

        _host.AddInfoBar(element);

        static IVsInfoBarActionItem ToActionItem(InfoBarUI item)
        {
            return item.Kind switch
            {
                InfoBarUIKind.Button => new InfoBarButton(item.Title),
                InfoBarUIKind.Hyperlink => new InfoBarHyperlink(item.Title),
                _ => throw Assumes.NotReachable()
            };
        }

        void OnClosed(InfoBarEntry entry)
        {
            // Ensure we are called back on the correct thread, as we perform all other
            // collection mutations on the UI thread.
            threadingService.VerifyOnUIThread();

            _entries.Remove(entry);
        }

        IVsInfoBarHost? FindMainWindowInfoBarHost()
        {
            if (vsShell.Value is null)
            {
                return null;
            }

            if (ErrorHandler.Failed(vsShell.Value.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out object mainWindowInfoBarHost)))
            {
                return null;
            }

            return mainWindowInfoBarHost as IVsInfoBarHost;
        }
    }
}
