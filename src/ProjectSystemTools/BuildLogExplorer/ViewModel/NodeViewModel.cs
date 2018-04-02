// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.LogModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer.ViewModel
{
    internal abstract class NodeViewModel : BaseViewModel
    {
        protected abstract Node Node { get; }

        public virtual bool IsPrimary => false;

        public string Elapsed => $"{Node.EndTime - Node.StartTime:mm':'ss'.'ff}";

        public Result Result => Node.Result;
    }
}
