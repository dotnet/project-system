// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectTreeCustomizablePropertyContextFactory
    {
        public static IProjectTreeCustomizablePropertyContext Create()
        {
            return Mock.Of<IProjectTreeCustomizablePropertyContext>();
        }
    }
}
