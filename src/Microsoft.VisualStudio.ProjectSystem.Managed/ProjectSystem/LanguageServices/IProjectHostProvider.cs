// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Creates the VS specific project host (IVSHierarchy) for a specified <see cref="UnconfiguredProject"/>.
    /// </summary>
    internal interface IProjectHostProvider
    {
        object GetProjectHostObject(UnconfiguredProject project);
    }
}
