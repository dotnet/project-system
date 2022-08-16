// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UI.InfoBarService;

internal partial class VsInfoBarService
{
    private class InfoBarEntry : IVsInfoBarUIEvents
    {
        private readonly IVsInfoBarUIElement _element;
        private readonly InfoBarUI[] _items;
        private readonly Action<InfoBarEntry> _onClose;
        private readonly uint _cookie;

        public InfoBarEntry(string message, IVsInfoBarUIElement element, InfoBarUI[] items, Action<InfoBarEntry> onClose)
        {
            Assumes.NotNull(message);
            Assumes.NotNull(element);
            Assumes.NotNull(items);
            Assumes.NotNull(onClose);

            Message = message;
            _element = element;
            _items = items;
            _onClose = onClose;
            element.Advise(this, out _cookie);
        }

        public string Message { get; }

        public void Close()
        {
            _element.Close();
        }

        public void OnActionItemClicked(IVsInfoBarUIElement element, IVsInfoBarActionItem actionItem)
        {
            Requires.NotNull(element, nameof(element));
            Requires.NotNull(actionItem, nameof(actionItem));

            InfoBarUI item = _items.First(i => i.Title == actionItem.Text);
            item.Action();

            if (item.CloseAfterAction)
            {
                Close();
            }
        }

        public void OnClosed(IVsInfoBarUIElement element)
        {
            Requires.NotNull(element, nameof(element));

            _element.Unadvise(_cookie);
            _onClose(this);
        }
    }
}

