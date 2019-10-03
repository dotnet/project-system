// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Moq;

namespace Microsoft.VisualStudio
{
    internal static class IHttpClientFactory
    {
        public static IHttpClient Create(string @string)
        {
            var mock = new Mock<IHttpClient>();
            mock.Setup(s => s.GetStringAsync(It.IsAny<Uri>()))
                .ReturnsAsync(() => @string);
            return mock.Object;
        }
    }
}
