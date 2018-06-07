// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    /// Language service host object for a configured project.
    /// </summary>
    internal interface IConfiguredProjectHostObject
    {
        /// <summary>
        ///     Gets a value containing a unique identifier of the <see cref="ConfiguredProject"/> instance's
        ///     <see cref="IWorkspaceProjectContext"/>.
        /// </summary>
        string WorkspaceProjectContextId { get; }
    }
}
