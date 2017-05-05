// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides properties for retrieving options for the project system.
    /// </summary>
    internal interface IProjectSystemOptions
    {
        /// <summary>
        ///     Gets a value indicating if the project output pane is enabled.
        /// </summary>
        /// <value>
        ///     <see langword="true"/> if the project output pane is enabled; otherwise, <see langword="false"/>.
        /// </value>
        bool IsProjectOutputPaneEnabled
        {
            get;
        }
    }
}
