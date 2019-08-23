// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    internal static class HandlerServices
    {
        /// <summary>
        ///     Normalizes <see cref="IProjectChangeDiff.RenamedItems"/> to <see cref="IProjectChangeDiff.AddedItems"/>
        ///     and <see cref="IProjectChangeDiff.RemovedItems"/>.
        /// </summary>
        public static IProjectChangeDiff NormalizeRenames(IProjectChangeDiff difference)
        {
            // Optimize for common case
            if (difference.RenamedItems.Count == 0)
                return difference;

            // Treat renamed items as just as an Add and Remove, makes finding conflicts easier
            IEnumerable<string> renamedNewNames = difference.RenamedItems.Select(r => r.Value);
            IEnumerable<string> renamedOldNames = difference.RenamedItems.Select(e => e.Key);

            IImmutableSet<string> added = difference.AddedItems.Union(renamedNewNames);
            IImmutableSet<string> removed = difference.RemovedItems.Union(renamedOldNames);

            return new ProjectChangeDiff(added, removed, difference.ChangedItems);
        }
    }
}
