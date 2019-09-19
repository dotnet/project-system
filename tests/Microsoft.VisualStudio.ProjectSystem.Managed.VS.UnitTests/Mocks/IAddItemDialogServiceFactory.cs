// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

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
