// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel.Builder
{
    internal sealed class DiagnosticInfo : MessageInfo
    {
        public bool IsError { get; }
        public string Code { get; }
        public int ColumnNumber { get; }
        public int EndColumnNumber { get; }
        public int LineNumber { get; }
        public int EndLineNumber { get; }
        public string File { get; }
        public string ProjectFile { get; }
        public string Subcategory { get; }

        public DiagnosticInfo(bool isError, string text, DateTime timestamp, string code, int columnNumber, int endColumnNumber, int lineNumber, int endLineNumber, string file, string projectFile, string subcategory) :
            base(text, timestamp)
        {
            IsError = isError;
            Code = code;
            ColumnNumber = columnNumber;
            EndColumnNumber = endColumnNumber;
            LineNumber = lineNumber;
            EndLineNumber = endLineNumber;
            File = file;
            ProjectFile = projectFile;
            Subcategory = subcategory;
        }
    }
}
