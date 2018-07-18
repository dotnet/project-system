// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal partial class WorkspaceProjectContextCreator
    {
        private class NullWorkspaceProjectContext : IWorkspaceProjectContext
        {
            public static readonly IWorkspaceProjectContext Instance = new NullWorkspaceProjectContext();

            public string DisplayName { get; set; }
            public string ProjectFilePath { get; set; }
            public Guid Guid { get; set; }
            public bool LastDesignTimeBuildSucceeded { get; set; }
            public string BinOutputPath { get; set; }

            public void AddAdditionalFile(string filePath, bool isInCurrentContext = true)
            {
            }

            public void AddAnalyzerReference(string referencePath)
            {
            }

            public void AddMetadataReference(string referencePath, MetadataReferenceProperties properties)
            {
            }

            public void AddProjectReference(IWorkspaceProjectContext project, MetadataReferenceProperties properties)
            {
            }

            public void AddSourceFile(string filePath, bool isInCurrentContext = true, IEnumerable<string> folderNames = null, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular)
            {
            }

            public void Dispose()
            {
            }

            public void RemoveAdditionalFile(string filePath)
            {
            }

            public void RemoveAnalyzerReference(string referencePath)
            {
            }

            public void RemoveMetadataReference(string referencePath)
            {
            }

            public void RemoveProjectReference(IWorkspaceProjectContext project)
            {
            }

            public void RemoveSourceFile(string filePath)
            {
            }

            public void SetOptions(string commandLineForOptions)
            {
            }

            public void SetRuleSetFile(string filePath)
            {
            }
        }
    }
}
