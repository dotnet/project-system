// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IProjectChangeDiffFactory
    {
        public static IProjectChangeDiff Create()
        {
            return Mock.Of<IProjectChangeDiff>();
        }

        public static IProjectChangeDiff Implement(IEnumerable<string> addedItems = null,
                                                   IEnumerable<string> changedItems = null,
                                                   IEnumerable<string> removedItems = null,
                                                   IDictionary<string, string> renamedItems = null,
                                                   IEnumerable<string> changedProperties = null,
                                                   bool? anyChanges = null,
                                                   MockBehavior? mockBehavior = null)
        {
            var behavior = mockBehavior ?? MockBehavior.Default;
            var mock = new Mock<IProjectChangeDiff>(behavior);

            if (addedItems != null)
            {
                mock.Setup(x => x.AddedItems).Returns(addedItems.ToImmutableSortedSet());
            }

            if (changedItems != null)
            {
                mock.Setup(x => x.ChangedItems).Returns(changedItems.ToImmutableSortedSet());
            }

            if (removedItems != null)
            {
                mock.Setup(x => x.RemovedItems).Returns(removedItems.ToImmutableSortedSet());
            }

            if (renamedItems != null)
            {
                mock.Setup(x => x.RenamedItems).Returns(renamedItems.ToImmutableDictionary());
            }

            if (changedProperties != null)
            {
                mock.Setup(x => x.ChangedProperties).Returns(changedProperties.ToImmutableSortedSet());
            }

            if (anyChanges.HasValue)
            {
                mock.Setup(x => x.AnyChanges).Returns(anyChanges.Value);
            }

            return mock.Object;
        }

        public static IProjectChangeDiff FromJson(string jsonString)
        {
            var model = new IProjectChangeDiffModel();
            return model.FromJson(jsonString);
        }
    }

    internal class IProjectChangeDiffModel : JsonModel<IProjectChangeDiff>, IProjectChangeDiff
    {
        public IImmutableSet<string> AddedItems { get; set; }
        public bool AnyChanges { get; set; }
        public IImmutableSet<string> ChangedItems { get; set; }
        public IImmutableSet<string> ChangedProperties { get; set; }
        public IImmutableSet<string> RemovedItems { get; set; }
        public IImmutableDictionary<string, string> RenamedItems { get; set; }

        public override IProjectChangeDiff ToActualModel()
        {
            return this;
        }
    }
}