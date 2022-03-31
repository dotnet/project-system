// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectChangeDiffFactory
    {
        public static IProjectChangeDiff WithAddedItems(string semiColonSeparatedItems)
        {
            if (semiColonSeparatedItems.Length == 0)
                return WithNoChanges();

            return WithAddedItems(semiColonSeparatedItems.Split(';'));
        }

        public static IProjectChangeDiff WithAddedItems(params string[] addedItems)
        {
            return Create(addedItems: ImmutableHashSet.Create(StringComparers.Paths, addedItems));
        }

        public static IProjectChangeDiff WithRemovedItems(string semiColonSeparatedItems)
        {
            if (semiColonSeparatedItems.Length == 0)
                return WithNoChanges();

            return WithRemovedItems(semiColonSeparatedItems.Split(';'));
        }

        public static IProjectChangeDiff WithRemovedItems(params string[] removedItems)
        {
            return Create(removedItems: ImmutableHashSet.Create(StringComparers.Paths, removedItems));
        }

        public static IProjectChangeDiff WithRenamedItems(string semiColonSeparatedOriginalNames, string semiColonSeparatedNewNames)
        {
            string[] originalNames = semiColonSeparatedOriginalNames.Split(';');
            string[] newNames = semiColonSeparatedNewNames.Split(';');

            var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);

            for (int i = 0; i < originalNames.Length; i++)
            {
                builder.Add(originalNames[i], newNames[i]);
            }

            return Create(renamedItems: builder.ToImmutable());
        }

        public static IProjectChangeDiff WithChangedItems(params string[] changedItems)
        {
            return Create(changedItems: ImmutableHashSet.Create(StringComparers.Paths, changedItems));
        }

        public static IProjectChangeDiff WithNoChanges()
        {
            return new ProjectChangeDiff();
        }

        public static IProjectChangeDiff Create(IImmutableSet<string>? addedItems = null, IImmutableSet<string>? removedItems = null, IImmutableSet<string>? changedItems = null, IImmutableDictionary<string, string>? renamedItems = null)
        {
            return new ProjectChangeDiff(addedItems, removedItems, changedItems, renamedItems);
        }

        public static IProjectChangeDiff FromJson(string jsonString)
        {
            var model = new IProjectChangeDiffModel();
            return model.FromJson(jsonString);
        }
    }

    internal class IProjectChangeDiffModel : JsonModel<IProjectChangeDiff>, IProjectChangeDiff
    {
        public IImmutableSet<string> AddedItems { get; set; } = ImmutableHashSet<string>.Empty;
        public bool AnyChanges { get; set; }
        public IImmutableSet<string> ChangedItems { get; set; } = ImmutableHashSet<string>.Empty;
        public IImmutableSet<string> ChangedProperties { get; set; } = ImmutableHashSet<string>.Empty;
        public IImmutableSet<string> RemovedItems { get; set; } = ImmutableHashSet<string>.Empty;
        public IImmutableDictionary<string, string> RenamedItems { get; set; } = ImmutableDictionary<string, string>.Empty;

        public override IProjectChangeDiff ToActualModel()
        {
            return this;
        }
    }
}
