// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    internal static class IAppDesignerFolderSpecialFileProviderFactory
    {
        public static IAppDesignerFolderSpecialFileProvider ImplementGetFile(string? result)
        {
            var mock = new Mock<IAppDesignerFolderSpecialFileProvider>();
            mock.Setup(m => m.GetFileAsync(It.IsAny<SpecialFiles>(), It.IsAny<SpecialFileFlags>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            return mock.Object;
        }
    }
}
