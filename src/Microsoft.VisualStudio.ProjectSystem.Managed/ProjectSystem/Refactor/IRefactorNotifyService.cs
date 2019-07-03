// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Refactor
{
    internal interface IRefactorNotifyService
    {
        Task TryOnBeforeGlobalSymbolRenamedAsync(string projectPath, IEnumerable<string> filePaths, string rqName, string newName);
        Task TryOnAfterGlobalSymbolRenamedAsync(string projectPath, IEnumerable<string> filePaths, string rqName, string newName);
    }
}
