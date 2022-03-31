// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
