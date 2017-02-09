// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    internal static class IEnumerableExtensions
    {
        internal static T FindNode<T>(this IEnumerable<T> nodes, string itemSpec, string itemType)
            where T : IDependencyNode
        {
            return nodes.FirstOrDefault(
                            x => x.Id.ItemSpec.Equals(itemSpec, StringComparison.OrdinalIgnoreCase)
                                 && x.Id.ItemType.Equals(itemType, StringComparison.OrdinalIgnoreCase));
        }

        internal static T FindNode<T>(this IEnumerable<T> nodes, string itemSpec)
            where T : IDependencyNode
        {
            return nodes.FirstOrDefault(
                            x => x.Id.ItemSpec.Equals(itemSpec, StringComparison.OrdinalIgnoreCase));
        }

    }
}
