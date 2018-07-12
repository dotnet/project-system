// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using EnvDTE;

using EnvDTE80;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides access to common Visual Studio automation objects.
    /// </summary>
    internal interface IDteServices
    {
        /// <summary>
        ///     Gets the top-level object in the Visual Studio automation object model.
        /// </summary>
        /// <exception cref="COMException">
        ///     This property was not accessed from the UI thread.
        /// </exception>
        DTE2 Dte
        {
            get;
        }

        /// <summary>
        ///     Gets the current solution.
        /// </summary>
        /// <exception cref="COMException">
        ///     This property was not accessed from the UI thread.
        /// </exception>
        Solution2 Solution
        {
            get;
        }

        /// <summary>
        ///     Gets the current project.
        /// </summary>
        /// <exception cref="COMException">
        ///     This property was not accessed from the UI thread.
        /// </exception>
        Project Project
        {

            get;
        }
    }
}
