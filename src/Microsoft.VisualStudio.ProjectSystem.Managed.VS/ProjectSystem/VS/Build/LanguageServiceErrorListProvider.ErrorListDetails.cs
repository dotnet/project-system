// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Build.Framework;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build
{
    partial class LanguageServiceErrorListProvider
    {
        /// <summary>
        /// Captures details from various types of MSBuild events.
        /// </summary>
        internal class ErrorListDetails
        {
            public ErrorListDetails()
            {
            }

            public string Message
            {
                get;
                set;
            }

            public string ProjectFile
            {
                get;
                set;
            }

            public string File
            {
                get;
                set;
            }

            public string FileFullPath
            {
                get
                {
                    if (!string.IsNullOrEmpty(this.ProjectFile) && !string.IsNullOrEmpty(this.File))
                    {
                        return TryMakeRooted(this.ProjectFile, this.File);
                    }

                    return string.Empty;
                }
            }

            public int LineNumber
            {
                get;
                set;
            }


            public int EndLineNumber
            {
                get;
                set;
            }

            /// <summary>
            /// Gets the line number that should be reported to the VS error list.
            /// (<see cref="LineNumber"/> - 1) to account for +1 that the error list applies.
            /// </summary>
            public int LineNumberForErrorList
            {
                get
                {
                    // The VS error list uses 0-based line numbers so a -1 adjustment needs to be made.
                    // It's weird.  We report "12" and they'll display "13".
                    return this.LineNumber > 0 ? this.LineNumber - 1 : 0;
                }
            }

            /// <summary>
            /// Gets the line number that should be reported to the VS error list.
            /// (<see cref="LineNumber"/> - 1) to account for +1 that the error list applies.
            /// </summary>
            public int EndLineNumberForErrorList
            {
                get
                {
                    // The VS error list uses 0-based line numbers so a -1 adjustment needs to be made.
                    // It's weird.  We report "12" and they'll display "13".
                    return (this.EndLineNumber > LineNumber && this.EndLineNumber > 0) ? this.EndLineNumber - 1 : LineNumberForErrorList;
                }
            }

            public int ColumnNumber
            {
                get;
                set;
            }

            public int EndColumnNumber
            {
                get;
                set;
            }

            /// <summary>
            /// Gets the column number that should be reported to the VS error list.
            /// (<see cref="ColumnNumber"/> - 1) to account for +1 that the error list applies.
            /// See <see cref="LineNumberForErrorList"/>, too.
            /// </summary>
            public int ColumnNumberForErrorList
            {
                get { return this.ColumnNumber > 0 ? this.ColumnNumber - 1 : 0; }
            }

            /// <summary>
            /// Gets the column number that should be reported to the VS error list.
            /// (<see cref="ColumnNumber"/> - 1) to account for +1 that the error list applies.
            /// See <see cref="LineNumberForErrorList"/>, too.
            /// </summary>
            public int EndColumnNumberForErrorList
            {
                get { return (this.EndColumnNumber > ColumnNumber && this.EndColumnNumber > 0) ? this.EndColumnNumber - 1 : ColumnNumberForErrorList; }
            }

            public string Code
            {
                get;
                set;
            }

            public VSTASKPRIORITY Priority
            {
                get;
                set;
            }

            /// <summary>
            /// Makes the specified path absolute if possible, otherwise return an empty string.
            /// </summary>
            /// <param name="basePath">The path used as the root if <paramref name="path"/> is relative.</param>
            /// <param name="path">An absolute or relative path.</param>
            /// <returns>An absolute path, or the empty string if <paramref name="path"/> invalid.</returns>
            private static string TryMakeRooted(string basePath, string path)
            {
                Requires.NotNullOrEmpty(basePath, nameof(basePath));
                Requires.NotNullOrEmpty(path, nameof(path));

                try
                {
                    return PathHelper.MakeRooted(basePath, path);
                }
                catch (ArgumentException)
                {
                    return string.Empty;
                }
            }
        }
    }
}
