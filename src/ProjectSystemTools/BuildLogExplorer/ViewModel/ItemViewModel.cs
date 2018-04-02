// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.ProjectSystem.LogModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer.ViewModel
{
    internal sealed class ItemViewModel : BaseViewModel
    {
        private readonly Item _item;
        private SelectedObjectWrapper _properties;

        public override string Text => _item.Name;

        public override SelectedObjectWrapper Properties => _properties ?? (_properties =
            new SelectedObjectWrapper(
                _item.Name,
                "Item",
                null,
                new Dictionary<string, IDictionary<string, string>> {{"Metadata", _item.Metadata}}));

        public ItemViewModel(Item item)
        {
            _item = item;
        }
    }
}
