// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    internal static class KnownEventArgs
    {
        public static PropertyChangedEventArgs TextPropertyChanged { get; } = new PropertyChangedEventArgs(nameof(ITreeDisplayItem.Text));

        public static PropertyChangedEventArgs IsUpdatingItemsPropertyChanged { get; } = new PropertyChangedEventArgs(nameof(IAsyncAttachedCollectionSource.IsUpdatingHasItems));

        public static PropertyChangedEventArgs HasItemsPropertyChanged { get; } = new PropertyChangedEventArgs(nameof(IAttachedCollectionSource.HasItems));
    }
}
