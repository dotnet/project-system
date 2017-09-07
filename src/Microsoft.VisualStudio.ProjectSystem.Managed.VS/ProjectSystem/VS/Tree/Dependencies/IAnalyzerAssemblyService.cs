// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    internal interface IAnalyzerAssemblyService
    {
        /// <summary>
        /// Checks if an assembly contains any diagnostic analyzers.
        /// </summary>
        /// <param name="fullPathToAssembly">Full path to the assembly to check.</param>
        /// <returns><code>true</code> if the assembly contains any types tagged with the
        /// <see cref="CodeAnalysis.Diagnostics.DiagnosticAnalyzerAttribute"/>.</returns>
        bool ContainsDiagnosticAnalyzers(string fullPathToAssembly);
    }
}
