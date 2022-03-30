// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
