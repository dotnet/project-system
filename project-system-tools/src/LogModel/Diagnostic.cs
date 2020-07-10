// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel
{
    public sealed class Diagnostic : Message
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

        public Diagnostic(bool isError, string text, DateTime timestamp, string code, int columnNumber, int endColumnNumber, int lineNumber, int endLineNumber, string file, string projectFile, string subcategory) :
            base(timestamp, text)
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

        public override string ToString()
        {
            var file = File ?? "";

            var position = "";
            if (LineNumber != 0 || ColumnNumber != 0)
            {
                position = $"({LineNumber},{ColumnNumber}):";
            }

            var code = "";
            if (!string.IsNullOrWhiteSpace(Code))
            {
                code = $" {GetType().Name.ToLowerInvariant()} {Code}:";
            }

            var text = Text;
            if (file.Length + position.Length + code.Length > 0)
            {
                text = " " + text;
            }

            var projectFile = "";
            if (!string.IsNullOrWhiteSpace(ProjectFile))
            {
                projectFile = $" [{ProjectFile}]";
            }

            return $"{file}{position}{code}{text}{projectFile}";
        }
    }
}
