// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
