// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectSpecificEditorProviderFactory
    {
        public static IProjectSpecificEditorProvider ImplementGetSpecificEditorAsync(Guid editorFactory = default)
        {
            var mock = new Mock<IProjectSpecificEditorInfo>();
            mock.Setup(i => i.EditorFactory)
                .Returns(editorFactory);

            return ImplementGetSpecificEditorAsync(mock.Object);
        }

        public static IProjectSpecificEditorProvider ImplementGetSpecificEditorAsync(IProjectSpecificEditorInfo projectSpecificEditorInfo)
        {
            var mock = new Mock<IProjectSpecificEditorProvider>();
            mock.Setup(p => p.GetSpecificEditorAsync(It.IsAny<string>()))
                .ReturnsAsync(projectSpecificEditorInfo);

            return mock.Object;
        }
    }
}
