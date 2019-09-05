// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.Refactor
{
    internal interface IRefactorNotifyService
    {
        void OnBeforeGlobalSymbolRenamed(string projectPath, IEnumerable<string> filePaths, string rqName, string newName);

        void OnAfterGlobalSymbolRenamed(string projectPath, IEnumerable<string> filePaths, string rqName, string newName);
    }
}
