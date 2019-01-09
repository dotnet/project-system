// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ProjectSystem;

using Moq;

namespace Microsoft.VisualStudio.LanguageServices.ProjectSystem
{
    internal static class IWorkspaceProjectContextMockFactory
    {
        public static IWorkspaceProjectContext ImplementDispose(Action action)
        {
            var mock = new Mock<IWorkspaceProjectContext>();

            mock.Setup(c => c.Dispose())
                .Callback(action);

            return mock.Object;
        }

        public static IWorkspaceProjectContext Create()
        {
            return new IWorkspaceProjectContextMock().Object;
        }

        public static IWorkspaceProjectContext CreateForSourceFiles(UnconfiguredProject project, Action<string> addSourceFile = null, Action<string> removeSourceFile = null)
        {
            var context = new IWorkspaceProjectContextMock();

            context.SetupGet(c => c.ProjectFilePath)
                .Returns(project.FullPath);

            if (addSourceFile != null)
            {
                context.Setup(c => c.AddSourceFile(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IEnumerable<string>>(), It.IsAny<SourceCodeKind>()))
                    .Callback<string, bool, IEnumerable<string>, SourceCodeKind>((p1, p2, p3, p4) => addSourceFile(p1));
            }

            if (removeSourceFile != null)
            {
                context.Setup(c => c.RemoveSourceFile(It.IsAny<string>()))
                    .Callback<string>(p1 => removeSourceFile(p1));
            }

            return context.Object;
        }

        public static IWorkspaceProjectContext CreateForMetadataReferences(UnconfiguredProject project, Action<string> addMetadataReference = null, Action<string> removeMetadataReference = null)
        {
            var context = new Mock<IWorkspaceProjectContext>();

            context.SetupGet(c => c.ProjectFilePath)
                .Returns(project.FullPath);

            if (addMetadataReference != null)
            {
                context.Setup(c => c.AddMetadataReference(It.IsAny<string>(), It.IsAny<MetadataReferenceProperties>()))
                    .Callback<string, MetadataReferenceProperties>((p1, p2) => addMetadataReference(p1));
            }

            if (removeMetadataReference != null)
            {
                context.Setup(c => c.RemoveMetadataReference(It.IsAny<string>()))
                    .Callback<string>(p1 => removeMetadataReference(p1));
            }

            return context.Object;
        }
    }
}
