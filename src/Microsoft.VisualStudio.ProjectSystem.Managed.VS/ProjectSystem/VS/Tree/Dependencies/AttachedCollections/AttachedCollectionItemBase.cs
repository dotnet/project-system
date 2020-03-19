// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Base class for attached items in the dependencies tree in Solution Explorer.
    /// </summary>
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
            typeof(IBrowsablePattern),
        };

        protected AttachedCollectionItemBase(string name) => Text = name;

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
    }
}
