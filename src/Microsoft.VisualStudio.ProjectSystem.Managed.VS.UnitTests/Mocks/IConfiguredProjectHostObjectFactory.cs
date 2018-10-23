// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.Interop;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal static class IConfiguredProjectHostObjectFactory
    {
        public static IConfiguredProjectHostObject Create()
        {
            var mock = new Mock<IVsHierarchy>();

            return mock.As<IConfiguredProjectHostObject>()
                       .Object;
        }
    }
}
