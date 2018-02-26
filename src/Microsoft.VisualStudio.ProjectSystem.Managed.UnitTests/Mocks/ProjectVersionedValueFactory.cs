// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class ProjectVersionedValueFactory
    {
        public static ProjectVersionedValue<T> Create<T>(T value)
        {
            return new ProjectVersionedValue<T>(value, ImmutableDictionary<NamedIdentity, IComparable>.Empty);
        }
    }
}
