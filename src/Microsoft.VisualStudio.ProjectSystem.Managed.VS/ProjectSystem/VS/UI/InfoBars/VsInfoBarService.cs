// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UI.InfoBars;

/// <summary>
/// Implementation of <see cref="IInfoBarService"/> that pushes messages to the info bar attached to Visual Studio's main window.
/// </summary>
[Export(typeof(IInfoBarService))]
internal sealed class VsInfoBarService : IInfoBarService
{
    // This component exists per configured project. We track entries at the global level here, and keep track of the
    // projects related to each info bar within the entry. This allows us to close info bars when all projects that
    // requested them are unloaded.
    private static readonly List<InfoBar> s_entries = [];

    private static IVsInfoBarHost? s_host;

    private readonly UnconfiguredProject _project;
    private readonly IProjectThreadingService _threadingService;
    private readonly IVsShellServices _vsShellServices;
    private readonly IVsUIService<SVsShell, IVsShell> _vsShell;
    private readonly IVsUIService<SVsInfoBarUIFactory, IVsInfoBarUIFactory> _vsInfoBarFactory;

    [ImportingConstructor]
    public VsInfoBarService(
        UnconfiguredProject project,
        IUnconfiguredProjectTasksService unconfiguredProjectTasksService,
        IProjectThreadingService threadingService,
        IVsShellServices vsShellServices,
        IVsUIService<SVsShell, IVsShell> vsShell,
        IVsUIService<SVsInfoBarUIFactory, IVsInfoBarUIFactory> vsInfoBarFactory)
    {
        _project = project;
        _threadingService = threadingService;
        _vsShellServices = vsShellServices;
        _vsShell = vsShell;
        _vsInfoBarFactory = vsInfoBarFactory;

        // Ensure we clean up when the project unloads. If it has already unloaded when
        // we register this callback, it is called immediately.
        unconfiguredProjectTasksService.UnloadCancellationToken.Register(() =>
        {
            lock (s_entries)
            {
                // Copy the list to avoid "collection modified during enumeration" exceptions,
                // as when the last project associated with an info bar is closed, we will
                // remove that entry from the list.
                foreach (InfoBar entry in s_entries.ToList())
                {
                    entry.OnProjectClosed(_project);
                }
            }
        });
    }

    public async Task ShowInfoBarAsync(string message, ImageMoniker image, CancellationToken cancellationToken, params ImmutableArray<InfoBarAction> actions)
    {
        if (await _vsShellServices.IsCommandLineModeAsync(cancellationToken))
        {
            // We don't want to show info bars in command line mode, as there's no GUI.
            return;
        }

        await _threadingService.SwitchToUIThread(cancellationToken);

        s_host ??= FindMainWindowInfoBarHost();

        if (_vsInfoBarFactory.Value is null || s_host is null)
        {
            return;
        }

        InfoBar infoBar = GetOrCreateInfoBar();

        infoBar.RegisterProject(_project);

        InfoBar GetOrCreateInfoBar()
        {
            lock (s_entries)
            {
                // We deduplicate info bars by their message. A single project can report the same message multiple times,
                // or multiple projects can report the same message. In either case, we only want to show one info bar.
                // Note that only the first instance is used to construct the message, so any difference in UI items between
                // the first and subsequent instances will not be reflected in the info bar.
                InfoBar? infoBar = s_entries.FirstOrDefault(e => e.Message == message);

                if (infoBar is null)
                {
                    // Create a new info bar.
                    InfoBarModel infoBarModel = new(
                        message,
                        actions.Select(ToActionItem),
                        image,
                        isCloseButtonVisible: true);

                    IVsInfoBarUIElement element = _vsInfoBarFactory.Value.CreateInfoBar(infoBarModel);

                    infoBar = new(message, element, actions);

                    s_entries.Add(infoBar);

                    // Show it.
                    s_host!.AddInfoBar(element);
                }

                return infoBar;
            }

            static IVsInfoBarActionItem ToActionItem(InfoBarAction action)
            {
                return action.Kind switch
                {
                    InfoBarActionKind.Button => new InfoBarButton(action.Title),
                    InfoBarActionKind.Hyperlink => new InfoBarHyperlink(action.Title),
                    _ => throw Assumes.NotReachable()
                };
            }
        }

        IVsInfoBarHost? FindMainWindowInfoBarHost()
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
    }

    /// <summary>
    /// Tracks state of an info bar.
    /// </summary>
    private sealed class InfoBar : IVsInfoBarUIEvents
    {
        private readonly HashSet<UnconfiguredProject> _projects = [];

        private readonly IVsInfoBarUIElement _element;
        private readonly ImmutableArray<InfoBarAction> _items;
        private readonly uint _cookie;

        public InfoBar(string message, IVsInfoBarUIElement element, ImmutableArray<InfoBarAction> items)
        {
            Message = message;
            _element = element;
            _items = items;

            Verify.HResult(element.Advise(this, out _cookie));
        }

        public string Message { get; }

        private void Close()
        {
            // Request the element to close. This will, in turn, invoke back into our OnClosed method,
            // via the IVsInfoBarUIEvents interface, where we do further cleanup.
            _element.Close();
        }

        public void OnActionItemClicked(IVsInfoBarUIElement element, IVsInfoBarActionItem actionItem)
        {
            // Assumption is that title is enough to uniquely identify items.
            InfoBarAction item = _items.First(i => i.Title == actionItem.Text);

            item.Callback();

            if (item.CloseAfterAction)
            {
                Close();
            }
        }

        public void OnClosed(IVsInfoBarUIElement element)
        {
            _element.Unadvise(_cookie);

            lock (s_entries)
            {
                Assumes.True(s_entries.Remove(this));
            }

            _projects.Clear();
        }

        internal void RegisterProject(UnconfiguredProject project)
        {
            _projects.Add(project);
        }

        internal void OnProjectClosed(UnconfiguredProject project)
        {
            if (_projects.Remove(project) && _projects.Count is 0)
            {
                // All projects that created this info bar have been unloaded.
                // Close the info bar and clean up.
                Close();
            }
        }
    }
}
