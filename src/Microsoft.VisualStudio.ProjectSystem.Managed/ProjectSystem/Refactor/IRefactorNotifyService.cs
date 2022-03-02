// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.Refactor
{
    internal interface IRefactorNotifyService
    {
        void OnBeforeGlobalSymbolRenamed(string projectPath, IEnumerable<string> filePaths, string rqName, string newName);

        void OnAfterGlobalSymbolRenamed(string projectPath, IEnumerable<string> filePaths, string rqName, string newName);
    }
}
