// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.LogModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor.ViewModel
{
    internal abstract class NodeViewModel : BaseViewModel
    {
        protected abstract Node Node { get; }

        public virtual bool IsPrimary => false;

        public string Elapsed => $"{Node.EndTime - Node.StartTime:mm':'ss'.'ffff}";

        public Result Result => Node.Result;
    }
}
