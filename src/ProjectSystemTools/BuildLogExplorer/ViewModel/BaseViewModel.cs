// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.LogModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer.ViewModel
{
    internal abstract class BaseViewModel
    {
        public abstract string Text { get; }

        public virtual IEnumerable<object> Children => Enumerable.Empty<object>();

        public virtual SelectedObjectWrapper Properties => null;

        protected static string FormatTime(Time time) =>
            $"In: {time.InclusiveTime:mm':'ss'.'ffff} | Ex: {time.ExclusiveTime:mm':'ss'.'ffff} | Hits: {time.NumberOfHits}";
    }
}
