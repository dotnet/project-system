// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;
using System;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal static class ILanguageServiceHostFactory
    {
        public static ILanguageServiceHost Create()
        {
            return Mock.Of<ILanguageServiceHost>();
        }

        public static ILanguageServiceHost ImplementHostSpecificErrorReporter(Func<object> action)
        {
            var mock = new Mock<ILanguageServiceHost>();
            mock.SetupGet(h => h.HostSpecificErrorReporter)
                .Returns(action);

            return mock.Object;
        }
    }
}