// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.VisualStudio.ProjectSystem.LogModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor.ViewModel
{
    internal sealed class ItemViewModel : BaseViewModel
    {
        private readonly Item _item;
        private SelectedObjectWrapper _properties;

        public override string Text => _item.Name;

        public override SelectedObjectWrapper Properties => _properties ??=
            new SelectedObjectWrapper(
                _item.Name,
                "Item",
                null,
                new Dictionary<string, IDictionary<string, string>> {{"Metadata", _item.Metadata}});

        public ItemViewModel(Item item)
        {
            _item = item;
        }
    }
}
