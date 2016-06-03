// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal interface IVsEnvironmentServices
    {
        Task<bool> CheckPromptAsync(string promptMessage);

        void NotifyFailureAsync(string renamedString);

        Task<T> GetEnvironmentSettingAsync<T>(string category, string page, string property, T defaultValue);

        Task<bool> CheckPromptForRenameAsync(string promptMessage);

        Task<Solution> RenameSymbolAsync(Solution solution, ISymbol symbol, string newName);

        Task<bool> ApplyChangesToSolutionAsync(Workspace ws, Solution renamedSolution);
    }
}
