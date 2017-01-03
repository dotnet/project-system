// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal static class IWorkspaceProjectContextFactory
    {
        public static IWorkspaceProjectContext Create(UnconfiguredProject project, Action<string> addSourceFile = null, Action<string> removeSourceFile = null)
        {
            var context = new Mock<IWorkspaceProjectContext>();

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
    }
}
