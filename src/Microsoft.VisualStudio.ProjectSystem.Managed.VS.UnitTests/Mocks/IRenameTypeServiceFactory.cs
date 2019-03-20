// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IRenameTypeServiceFactory
    {
        public static IRenameTypeService Create(bool existsSymbolToRename = true)
        {
            var mock = new Mock<IRenameTypeService>();
            mock.Setup(h => h.AnyTypeToRenameAsync(It.IsAny<string>(),
                                                   It.IsAny<string>()))
                                                   .ReturnsAsync(existsSymbolToRename);
            return mock.Object;
        }
    }
}


