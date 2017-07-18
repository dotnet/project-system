// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    internal static class IProjectFileEditorPresenterFactory
    {
        public static IProjectFileEditorPresenter Create() => Mock.Of<IProjectFileEditorPresenter>();

        public static Lazy<IProjectFileEditorPresenter> CreateLazy() => new Lazy<IProjectFileEditorPresenter>(() => Create());

        public static IProjectFileEditorPresenter ImplementCloseWindowAsync(bool shouldClose)
        {
            var mock = new Mock<IProjectFileEditorPresenter>();
            mock.Setup(e => e.CanCloseWindowAsync()).Returns(Task.FromResult(shouldClose));
            return mock.Object;
        }
    }
}
