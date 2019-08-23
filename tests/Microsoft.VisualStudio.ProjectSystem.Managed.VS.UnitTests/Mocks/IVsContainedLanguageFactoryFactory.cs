// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.TextManager.Interop
{
    internal static class IVsContainedLanguageFactoryFactory
    {
        public static IVsContainedLanguageFactory Create()
        {
            return Mock.Of<IVsContainedLanguageFactory>();
        }
    }
}
