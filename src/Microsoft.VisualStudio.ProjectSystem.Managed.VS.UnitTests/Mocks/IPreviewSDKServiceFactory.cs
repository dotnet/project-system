// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.VS;
using Moq;

namespace Microsoft.VisualStudio
{
    internal class IPreviewSDKServiceFactory
    {
        internal static IPreviewSDKService Create(bool isPreviewSDKInUse = false)
        {
            var mock = new Mock<IPreviewSDKService>();
            mock.Setup(m => m.IsPreviewSDKInUseAsync()).ReturnsAsync(isPreviewSDKInUse);
            return mock.Object;
        }
    }
}
