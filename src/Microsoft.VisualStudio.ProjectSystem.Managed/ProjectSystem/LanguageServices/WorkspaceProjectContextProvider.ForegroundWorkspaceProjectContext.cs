// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal partial class WorkspaceProjectContextProvider
    {
        /// <summary>
        ///     Wraps <see cref="IWorkspaceProjectContext"/> and enforces that it's accessed only on the UI thread.
        /// </summary>
        private class ForegroundWorkspaceProjectContext : IWorkspaceProjectContext
        {
            private readonly IProjectThreadingService _threadingService;
            private readonly IWorkspaceProjectContext _underlyingContext;

            public ForegroundWorkspaceProjectContext(IProjectThreadingService threadingService, IWorkspaceProjectContext underlyingContext)
            {
                Assumes.NotNull(threadingService);
                Assumes.NotNull(underlyingContext);

                _threadingService = threadingService;
                _underlyingContext = underlyingContext;
            }

            public IWorkspaceProjectContext UnderlyingContext
            {
                get
                {
                    _threadingService.VerifyOnUIThread();

                    return _underlyingContext;
                }
            }

            public string DisplayName
            {
                get { return UnderlyingContext.DisplayName; }
                set { UnderlyingContext.DisplayName = value; }
            }

            public string ProjectFilePath
            {
                get { return UnderlyingContext.ProjectFilePath; }
                set { UnderlyingContext.ProjectFilePath = value; }
            }

            public Guid Guid
            {
                get { return UnderlyingContext.Guid; }
                set { UnderlyingContext.Guid = value; }
            }

            public bool LastDesignTimeBuildSucceeded
            {
                get { return UnderlyingContext.LastDesignTimeBuildSucceeded; }
                set { UnderlyingContext.LastDesignTimeBuildSucceeded = value; }
            }

            public string BinOutputPath
            {
                get { return UnderlyingContext.BinOutputPath; }
                set { UnderlyingContext.BinOutputPath = value; }
            }

            public ProjectId Id
            {
                get { return UnderlyingContext.Id; }
            }

            public void AddAdditionalFile(string filePath, bool isInCurrentContext = true)
            {
                UnderlyingContext.AddAdditionalFile(filePath, isInCurrentContext);
            }

            public void AddAnalyzerReference(string referencePath)
            {
                UnderlyingContext.AddAnalyzerReference(referencePath);
            }

            public void AddDynamicFile(string filePath, IEnumerable<string> folderNames = null)
            {
                UnderlyingContext.AddDynamicFile(filePath, folderNames);
            }

            public void AddMetadataReference(string referencePath, MetadataReferenceProperties properties)
            {
                UnderlyingContext.AddMetadataReference(referencePath, properties);
            }

            public void AddProjectReference(IWorkspaceProjectContext project, MetadataReferenceProperties properties)
            {
                UnderlyingContext.AddProjectReference(project, properties);
            }

            public void AddSourceFile(string filePath, bool isInCurrentContext = true, IEnumerable<string> folderNames = null, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular)
            {
                UnderlyingContext.AddSourceFile(filePath, isInCurrentContext, folderNames, sourceCodeKind);
            }

            public void Dispose()
            {
                UnderlyingContext.Dispose();
            }

            public void EndBatch()
            {
                UnderlyingContext.EndBatch();
            }

            public void RemoveAdditionalFile(string filePath)
            {
                UnderlyingContext.RemoveAdditionalFile(filePath);
            }

            public void RemoveAnalyzerReference(string referencePath)
            {
                UnderlyingContext.RemoveAnalyzerReference(referencePath);
            }

            public void RemoveDynamicFile(string fullPath)
            {
                UnderlyingContext.RemoveDynamicFile(fullPath);
            }

            public void RemoveMetadataReference(string referencePath)
            {
                UnderlyingContext.RemoveMetadataReference(referencePath);
            }

            public void RemoveProjectReference(IWorkspaceProjectContext project)
            {
                UnderlyingContext.RemoveProjectReference(project);
            }

            public void RemoveSourceFile(string filePath)
            {
                UnderlyingContext.RemoveSourceFile(filePath);
            }

            public void SetOptions(string commandLineForOptions)
            {
                UnderlyingContext.SetOptions(commandLineForOptions);
            }

            public void SetProperty(string name, string value)
            {
                UnderlyingContext.SetProperty(name, value);
            }

            public void SetRuleSetFile(string filePath)
            {
                UnderlyingContext.SetRuleSetFile(filePath);
            }

            public void StartBatch()
            {
                UnderlyingContext.StartBatch();
            }
        }
    }
}
