// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    /// <summary>
    ///     Represents a single key/value for a <see cref="IVsReferenceItem"/>.
    /// </summary>
    internal class ReferenceProperty : IVsReferenceProperty
    {
        public ReferenceProperty(string name, string value)
        {
            Requires.NotNullOrEmpty(name, nameof(name));
            Requires.NotNull(value, nameof(value));

            Name = name;
            Value = value;
        }

        public string Name { get; }

        public string Value { get; }
    }
}
