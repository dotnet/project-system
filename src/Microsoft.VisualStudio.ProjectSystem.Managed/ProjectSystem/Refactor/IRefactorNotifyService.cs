// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ProjectSystem.Refactor
{
    internal interface IRefactorNotifyService
    {
        Task<bool> TryOnBeforeGlobalSymbolRenamedAsync(string projectPath, IEnumerable<string> filePaths, string rqName, string newName);
        Task<bool> TryOnAfterGlobalSymbolRenamedAsync(string projectPath, IEnumerable<string> filePaths, string rqName, string newName);
    }
}
