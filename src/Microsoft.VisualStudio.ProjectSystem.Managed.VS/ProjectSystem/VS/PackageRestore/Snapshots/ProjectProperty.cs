// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    /// <summary>
    ///     Represents a single key/value for a <see cref="IVsTargetFrameworkInfo"/>.
    /// </summary>
    [DebuggerDisplay("{Name}: {Value}")]
    internal class ProjectProperty : IVsProjectProperty
    {
        public ProjectProperty(string name, string value)
        {
            Requires.NotNullOrEmpty(name, nameof(name));

            Name = name;
            Value = value;
        }

        public string Name { get; }

        public string Value { get; }
    }
}
