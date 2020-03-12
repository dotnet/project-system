// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
