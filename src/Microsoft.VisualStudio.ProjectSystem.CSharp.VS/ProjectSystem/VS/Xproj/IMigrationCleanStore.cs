// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    internal interface IMigrationCleanStore
    {
        /// <summary>
        /// The list of files that need to be cleaned, clearing the list after return.
        /// </summary>
        ISet<string> DrainFiles();

        /// <summary>
        /// Adds a new file to the list of files that need to be cleaned.
        /// </summary>
        void AddFiles(params string[] files);

    }
}
