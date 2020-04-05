// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Base class for attached items in the dependencies tree in Solution Explorer.
    /// </summary>
    /// <remarks>
    /// Subclasses should implement one or both of:
    /// <list type="bullet">
    ///     <item><see cref="IContainsAttachedItems"/> if, when expanded by the user, the item may have children.</item>
    ///     <item><see cref="IContainedByAttachedItems"/> if the item may appear in search results.</item>
    /// </list>
    /// </remarks>
    internal abstract class AttachedCollectionItemBase
        : ITreeDisplayItem,
          ITreeDisplayItemWithImages,
          IPrioritizedComparable,
          IInteractionPatternProvider,
          IBrowsablePattern
    {
        // Other patterns we may wish to utilise in future are:
        //
        // - IInvocationPattern
        // - IContextMenuPattern
        // - ISupportExpansionEvents
        // - ISupportExpansionState
        // - IDragDropSourcePattern
        // - IDragDropTargetPattern
        // - ISupportDisposalNotification
        // - IRenamePattern
        // - We also see requests for IVsHierarchyItem
        //
        // NOTE we don't have to support ITreeDisplayItemWithImages -- it's covered by ITreeDisplayItem

        private static readonly HashSet<Type> s_supportedPatterns = new HashSet<Type>
        {
            typeof(ITreeDisplayItem),
            typeof(IBrowsablePattern)
        };

        protected AttachedCollectionItemBase(string name)
        {
            Requires.NotNullOrWhiteSpace(name, nameof(name));

            Text = name;
        }

        public string Text { get; }

        public abstract int Priority { get; }

        public virtual FontStyle FontStyle => FontStyles.Normal;

        public virtual FontWeight FontWeight => FontWeights.Normal;

        public abstract ImageMoniker IconMoniker { get; }

        public virtual ImageMoniker ExpandedIconMoniker => IconMoniker;

        public virtual ImageMoniker OverlayIconMoniker => default;

        public virtual ImageMoniker StateIconMoniker => default;

        public virtual string? StateToolTipText => null;

        // Return null means ToolTipText is displayed only when the item's label is truncated
        public object? ToolTipContent => null;

        public string ToolTipText => Text;

        public bool IsCut => false;

        public virtual object? GetBrowseObject() => null;

        public TPattern? GetPattern<TPattern>() where TPattern : class
        {
            if (s_supportedPatterns.Contains(typeof(TPattern)))
            {
                return this as TPattern;
            }

            return null;
        }

        public int CompareTo(object obj)
        {
            if (obj is ITreeDisplayItem item)
            {
                // Order by caption
                return StringComparer.OrdinalIgnoreCase.Compare(Text, item.Text);
            }

            return 0;
        }

        public override string ToString() => Text;

        /// <summary>
        /// Simple implementation of <see cref="IAttachedCollectionSource"/> that provides an existing
        /// set of items (i.e. not lazily constructed), expected to be used primarily for
        /// <see cref="KnownRelationships.ContainedBy"/> queries of search result item's parentage.
        /// </summary>
        protected sealed class MaterializedAttachedCollectionSource : IAttachedCollectionSource
        {
            public MaterializedAttachedCollectionSource(object sourceItem, IEnumerable? items)
            {
                Requires.NotNull(sourceItem, nameof(sourceItem));

                SourceItem = sourceItem;
                Items = items;
            }

            public object SourceItem { get; }
            public bool HasItems => Items != null;
            public IEnumerable? Items { get; }
        }
    }
}
