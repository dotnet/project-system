// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    /// Language service host object for an unconfigured project.
    /// </summary>
    internal interface IUnconfiguredProjectHostObject : IDisposable
    {
        /// <summary>
        /// <see cref="IConfiguredProjectHostObject"/> for the active intellisense project.
        /// </summary>
        IConfiguredProjectHostObject ActiveIntellisenseProjectHostObject { get; set; }

        /// <summary>
        /// Flag indicating that we are currently disposing inner configured projects.
        /// </summary>
        /// <remarks>This property is to workaround Roslyn deadlock https://github.com/dotnet/roslyn/issues/14479. </remarks>
        bool DisposingConfiguredProjectHostObjects { get; set; }

        /// <summary>
        /// Pushes all the pending updates for active intellisense host objects while we were
        /// disposing configured projects.
        /// </summary>
        /// <remarks>This method is to workaround Roslyn deadlock https://github.com/dotnet/roslyn/issues/14479. </remarks>
        void PushPendingIntellisenseProjectHostObjectUpdates();
    }
}
