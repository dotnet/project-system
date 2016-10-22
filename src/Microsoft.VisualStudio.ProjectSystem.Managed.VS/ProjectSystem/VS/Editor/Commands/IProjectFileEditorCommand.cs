// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.Interop;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor.Commands
{
    internal interface IProjectFileEditorCommandAsync
    {
        long CommandId { get; }

        Task<int> Handle(IVsProject project);
    }
}
