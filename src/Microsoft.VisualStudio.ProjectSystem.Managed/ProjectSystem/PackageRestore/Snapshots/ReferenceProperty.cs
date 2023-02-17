// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
{
    /// <summary>
    ///     Represents a single key/value for a <see cref="ReferenceItem"/>.
    /// </summary>
    [DebuggerDisplay("{Name}: {Value}")]
    internal class ReferenceProperty
    {
        public ReferenceProperty(string name, string value)
        {
            Requires.NotNullOrEmpty(name);

            Name = name;
            Value = value;
        }

        public string Name { get; }

        public string Value { get; }
    }
}
