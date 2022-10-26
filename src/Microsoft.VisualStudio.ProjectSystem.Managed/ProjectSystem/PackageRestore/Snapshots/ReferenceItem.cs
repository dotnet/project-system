// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
{
    /// <summary>
    ///     Represents a single package, tool or project reference.
    /// </summary>
    [DebuggerDisplay("Name = {Name}")]
    internal class ReferenceItem : IVsReferenceItem
    {
        // If additional fields/properties are added to this class, please update RestoreHasher

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
