// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor.ViewModel
{
    internal sealed class ListViewModel<TItem> : BaseViewModel
    {
        private readonly IEnumerable<TItem> _list;
        private readonly Func<TItem, object> _selector;

        private object[] _children;

        public override string Text { get; }

        public override IEnumerable<object> Children => _children ?? (_children = _list.Select(_selector).ToArray());

        public ListViewModel(string name, IEnumerable<TItem> list, Func<TItem, object> selector)
        {
            Text = name;
            _list = list;
            _selector = selector;
        }
    }
}
