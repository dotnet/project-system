// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class SVsServiceProviderFactory
    {
        public static SVsServiceProvider Create(object? service = null)
        {
            var mock = new Mock<SVsServiceProvider>();
            mock.Setup<object?>(s => s.GetService(It.IsAny<Type>())).Returns(service);
            return mock.Object;
        }
    }
}
