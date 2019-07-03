﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class ISourceCodeControlIntegrationFactory
    {
        public static ISourceCodeControlIntegration Create()
        {
            return Mock.Of<ISourceCodeControlIntegration>();
        }
    }
}
