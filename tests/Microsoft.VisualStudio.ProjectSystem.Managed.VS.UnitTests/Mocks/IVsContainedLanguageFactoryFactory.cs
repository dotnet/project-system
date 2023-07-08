// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
