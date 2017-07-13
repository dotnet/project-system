// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.Shell.Interop
{
    /// <summary>
    ///     Provides extension methods for <see cref="IVsOutputWindowPane"/>.
    /// </summary>
    internal static class IVsOutputWindowPaneExtensions
    {
        /// <summary>
        ///     Prints text to the output window avoiding pushing a message pump, if possible.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="pane"/> is <see langword="null"/>.
        /// </exception>
        public static void OutputStringNoPump(this IVsOutputWindowPane pane, string pszOutputString)
        {
            Requires.NotNull(pane, nameof(pane));

            if (pane is IVsOutputWindowPaneNoPump noPumpPane)
            {
                noPumpPane.OutputStringNoPump(pszOutputString);
            }
            else
            { 
                HResult hr = pane.OutputStringThreadSafe(pszOutputString);
                if (hr.Failed)
                    throw hr.Exception;
            }
        }
    }
}
