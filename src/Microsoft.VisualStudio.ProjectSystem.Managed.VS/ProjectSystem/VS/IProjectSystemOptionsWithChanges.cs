// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.Settings;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    /// Provides properties for retrieving options for the project system and listening to option changes.
    /// </summary>
    internal interface IProjectSystemOptionsWithChanges : IProjectSystemOptions
    {
        /// <summary>
        /// Registers the given <paramref name="handler"/> as a callback for option changes.
        /// </summary>
        Task RegisterOptionChangedEventHandlerAsync(PropertyChangedAsyncEventHandler handler);

        /// <summary>
        /// Unregsiters the given <paramref name="handler"/> from option change callbacks.
        /// </summary>
        Task UnregisterOptionChangedEventHandlerAsync(PropertyChangedAsyncEventHandler handler);
    }
}
