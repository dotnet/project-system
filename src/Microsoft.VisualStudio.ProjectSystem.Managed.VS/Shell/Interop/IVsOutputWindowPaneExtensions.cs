// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
                noPumpPane.OutputStringNoPump(pszOutputString + Environment.NewLine);
            }
            else
            {
                Verify.HResult(pane.OutputStringThreadSafe(pszOutputString + Environment.NewLine));
            }
        }
    }
}
