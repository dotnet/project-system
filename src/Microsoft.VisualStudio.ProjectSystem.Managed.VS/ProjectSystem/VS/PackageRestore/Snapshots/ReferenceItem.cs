// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    /// <summary>
    ///     Represents a single package, tool or project reference.
    /// </summary>
    [DebuggerDisplay("Name = {Name}")]
    internal class ReferenceItem : IVsReferenceItem
    {
        public ReferenceItem(string name, IVsReferenceProperties properties)
        {
            Requires.NotNullOrEmpty(name, nameof(name));

            Name = name;
            Properties = properties;
        }

        public string Name { get; }

        public IVsReferenceProperties Properties { get; }
    }
}
