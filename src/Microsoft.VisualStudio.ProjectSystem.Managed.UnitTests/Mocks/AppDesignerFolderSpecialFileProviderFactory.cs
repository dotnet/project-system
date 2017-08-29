// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;
using System.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    internal class AppDesignerFolderSpecialFileProviderFactory
    {
        public static AppDesignerFolderSpecialFileProvider ImplementGetFile(string result)
        {
            var mock = new Mock<AppDesignerFolderSpecialFileProvider>();
            mock.Setup(m => m.GetFileAsync(It.IsAny<SpecialFiles>(), It.IsAny<SpecialFileFlags>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            return mock.Object;
        }
    }
}
