// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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

        public static IWorkspaceProjectContext ImplementSetProperty(Action<string, string> action)
        {
            var mock = new Mock<IWorkspaceProjectContext>();

            mock.Setup(c => c.SetProperty(It.IsAny<string>(), It.IsAny<string>()))
                .Callback(action);

            return mock.Object;
        }

        public static IWorkspaceProjectContext Create()
        {
            return new IWorkspaceProjectContextMock().Object;
        }

        public static IWorkspaceProjectContext CreateForDynamicFiles(UnconfiguredProject project, Action<string>? addDynamicFile = null)
        {
            var context = new IWorkspaceProjectContextMock();

            context.SetupGet(c => c.ProjectFilePath)
                .Returns(project.FullPath);

            if (addDynamicFile != null)
            {
                context.Setup(c => c.AddDynamicFile(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                    .Callback<string, IEnumerable<string>>((p1, p2) => addDynamicFile(p1));
            }

            return context.Object;
        }

        public static IWorkspaceProjectContext CreateForSourceFiles(UnconfiguredProject project, Action<string>? addSourceFile = null, Action<string>? removeSourceFile = null)
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
                    .Callback(removeSourceFile);
            }

            return context.Object;
        }

        public static IWorkspaceProjectContext CreateForMetadataReferences(UnconfiguredProject project, Action<string>? addMetadataReference = null, Action<string>? removeMetadataReference = null)
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
                    .Callback(removeMetadataReference);
            }

            return context.Object;
        }
    }
}
