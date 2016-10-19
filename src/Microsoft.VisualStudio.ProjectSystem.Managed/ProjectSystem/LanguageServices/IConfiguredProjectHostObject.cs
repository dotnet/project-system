// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    /// Language service host object for a configured project.
    /// </summary>
    internal interface IConfiguredProjectHostObject
    {
        /// <summary>
        /// Display name for the configured project.
        /// </summary>
        string ProjectDisplayName { get; }
    }
}
