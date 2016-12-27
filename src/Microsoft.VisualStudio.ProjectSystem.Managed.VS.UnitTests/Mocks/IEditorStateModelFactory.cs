// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    internal static class IEditorStateModelFactory
    {
        public static IEditorStateModel Create() => Mock.Of<IEditorStateModel>();

        public static IEditorStateModel ImplementCloseWindowAsync(bool shouldClose)
        {
            var mock = new Mock<IEditorStateModel>();
            mock.Setup(e => e.CloseWindowAsync()).Returns(Task.FromResult(shouldClose));
            return mock.Object;
        }
    }
}
