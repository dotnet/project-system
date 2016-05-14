// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Build.Framework;
using Microsoft.VisualStudio.Shell;
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
            private readonly string _helpKeyword;

            public ErrorListDetails(BuildEventArgs args = null)
            {
                this.Priority = VSTASKPRIORITY.TP_NORMAL;

                if (args != null)
                {
                    this._helpKeyword = args.HelpKeyword;
                    this.Message = args.Message;
                }
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

            public string ProjectDisplayName
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

            public string Subcategory
            {
                get;
                set;
            }

            public int LineNumber
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

            public int ColumnNumber
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

            public TaskPriority TaskPriority
            {
                get
                {
                    switch (this.Priority)
                    {
                        case VSTASKPRIORITY.TP_HIGH:
                            return Shell.TaskPriority.High;
                        case VSTASKPRIORITY.TP_LOW:
                            return Shell.TaskPriority.Low;
                        case VSTASKPRIORITY.TP_NORMAL:
                            return Shell.TaskPriority.Normal;
                        default:
                            Report.Fail("Unexpected VSTASKPRIORITY value: " + this.Priority);
                            return Shell.TaskPriority.Normal;
                    }
                }
            }

            public TaskErrorCategory ErrorCategory
            {
                get
                {
                    switch (this.Priority)
                    {
                        case VSTASKPRIORITY.TP_HIGH:
                            return TaskErrorCategory.Error;
                        case VSTASKPRIORITY.TP_NORMAL:
                            return TaskErrorCategory.Warning;
                        case VSTASKPRIORITY.TP_LOW:
                        default:
                            return TaskErrorCategory.Message;
                    }
                }
            }

            public __VSERRORCATEGORY VsErrorCategory
            {
                get
                {
                    switch (this.Priority)
                    {
                        case VSTASKPRIORITY.TP_HIGH:
                            return __VSERRORCATEGORY.EC_ERROR;
                        case VSTASKPRIORITY.TP_NORMAL:
                            return __VSERRORCATEGORY.EC_WARNING;
                        case VSTASKPRIORITY.TP_LOW:
                        default:
                            return __VSERRORCATEGORY.EC_MESSAGE;
                    }
                }
            }

            public string HelpKeyword
            {
                get { return string.IsNullOrEmpty(this._helpKeyword) ? this.Code : this._helpKeyword; }
            }

            public ErrorTaskMessageIdProvider MessageIdProvider
            {
                get;
                set;
            }

            public uint MessageId
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
                catch (FormatException)
                {
                    return string.Empty;
                }
            }
        }
    }
}
