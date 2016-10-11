// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    /// Language service host object for an unconfigured project.
    /// </summary>
    internal interface IUnconfiguredProjectHostObject
    {
        /// <summary>
        /// <see cref="IConfiguredProjectHostObject"/> for the active intellisense project.
        /// </summary>
        IConfiguredProjectHostObject ActiveIntellisenseProjectHostObject { get; set; }
    }
}
