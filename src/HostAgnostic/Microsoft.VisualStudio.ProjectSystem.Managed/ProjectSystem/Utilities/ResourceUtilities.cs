// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.VisualStudio.ProjectSystem.Utilities
{
    /// <summary>
    /// This class contains utility methods for dealing with resources.
    /// </summary>
    internal static class ResourceUtilities
    {
        /// <summary>
        /// Formats the given string using the variable arguments passed in.
        ///
        /// PERF WARNING: calling a method that takes a variable number of arguments is expensive, because memory is allocated for
        /// the array of arguments -- do not call this method repeatedly in performance-critical scenarios
        /// </summary>
        /// <remarks>This method is thread-safe.</remarks>
        /// <param name="unformatted">The string to format.</param>
        /// <param name="args">Optional arguments for formatting the given string.</param>
        /// <returns>The formatted string.</returns>
        internal static string FormatString(string unformatted, params object[] args)
        {
            string formatted = unformatted;

            // NOTE: String.Format() does not allow a null arguments array
            if ((args != null) && (args.Length > 0))
            {
#if DEBUG

#if VALIDATERESOURCESTRINGS
                // The code below reveals many places in our codebase where
                // we're not using all of the data given to us to format
                // strings -- but there are too many to presently fix.
                // Rather than toss away the code, we should later build it
                // and fix each offending resource (or the code processing
                // the resource).

                // String.Format() will throw a FormatException if args does
                // not have enough elements to match each format parameter.
                // However, it provides no feedback in the case when args contains
                // more elements than necessary to replace each format 
                // parameter.  We'd like to know if we're providing too much
                // data in cases like these, so we'll fail if this code runs.
                //
                // See DevDiv Bugs 15210 for more information.
                                
                // We create an array with one fewer element
                object[] trimmedArgs = new object[args.Length - 1];
                Array.Copy(args, 0, trimmedArgs, 0, args.Length - 1);

                bool caughtFormatException = false;
                try
                {
                    // This will throw if there aren't enough elements in trimmedArgs...
                    String.Format(CultureInfo.CurrentCulture, unformatted, trimmedArgs);
                }
                catch (FormatException)
                {
                    caughtFormatException = true;
                }

                // If we didn't catch an exception above, then some of the elements
                // of args were unnecessary when formatting unformatted...
                Debug.Assert
                (
                    caughtFormatException,
                    String.Format("The provided format string '{0}' had fewer format parameters than the number of format args, '{1}'.", unformatted, args.Length)
                );
#endif

#endif
                // Format the string, using the variable arguments passed in.
                // NOTE: all String methods are thread-safe
                formatted = string.Format(CultureInfo.CurrentCulture, unformatted, args);
            }

            return formatted;
        }

        #region Private Methods
        /// <summary>
        /// Retrieves the CPS F1-help keyword for the given resource string. Help keywords are used to index help topics in
        /// host IDEs.
        /// </summary>
        /// <param name="resourceName">Resource string to get the CPS F1-keyword for.</param>
        /// <returns>The CPS F1-help keyword string.</returns>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Is in fact called from multiple places")]
        private static string GetHelpKeyword(string resourceName)
        {
            return "CPS." + resourceName;
        }
        #endregion
    }
}
