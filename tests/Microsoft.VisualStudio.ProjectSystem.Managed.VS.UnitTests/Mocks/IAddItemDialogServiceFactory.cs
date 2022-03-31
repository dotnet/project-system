// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.UI
{
    internal static class IAddItemDialogServiceFactory
    {
        public static IAddItemDialogService Create()
        {
            var mock = new Mock<IAddItemDialogService>();
            mock.Setup(s => s.CanAddNewOrExistingItemTo(It.IsAny<IProjectTree>()))
                .Returns(true);

            return mock.Object;
        }

        public static IAddItemDialogService ImplementShowAddNewItemDialogAsync(Func<IProjectTree, string, string, bool> action)
        {
            var mock = new Mock<IAddItemDialogService>();
            mock.Setup(s => s.CanAddNewOrExistingItemTo(It.IsAny<IProjectTree>()))
                .Returns(true);

            mock.Setup(s => s.ShowAddNewItemDialogAsync(It.IsAny<IProjectTree>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(action);

            return mock.Object;
        }
    }
}
