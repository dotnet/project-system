// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Represents the physical project tree in Solution Explorer.
    /// </summary>
    internal interface IPhysicalProjectTree
    {
        /// <summary>
        ///     Gets the most recently published tree, or <see langword="null"/> if it has not yet be published.
        /// </summary>
        IProjectTree CurrentTree
        {
            get;
        }

        /// <summary>
        ///     Gets the service that manages the tree in Solution Explorer.
        /// </summary>
        IProjectTreeService TreeService
        {
            get;
        }

        /// <summary>
        ///     Gets the project tree provider that creates the Solution Explorer tree.
        /// </summary>
        IProjectTreeProvider TreeProvider
        {
            get;
        }
    }
}
