// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.Providers
{
    public interface ILoggingController
    {
        /// <summary>
        /// Is this logging controller currently collecting logs?
        /// true if yes, false if no.
        /// </summary>
        bool IsLogging { get; }

        /// <summary>
        /// Creates a new logger to collect builds and their logs
        /// </summary>
        /// <param name="isDesignTime">Is this for DesignTime build logging?</param>
        /// <returns>Return newly created logger</returns>
        ILogger CreateLogger(bool isDesignTime);
    }
}
