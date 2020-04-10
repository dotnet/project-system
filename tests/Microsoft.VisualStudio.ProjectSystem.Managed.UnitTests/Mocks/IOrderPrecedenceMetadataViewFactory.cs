// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IOrderPrecedenceMetadataViewFactory
    {
        public static IOrderPrecedenceMetadataView Create(string? appliesTo, int orderPrecedence = 0)
        {
            var mock = new Mock<IOrderPrecedenceMetadataView>();
            mock.SetupGet(v => v.AppliesTo)
                .Returns(appliesTo!);

            mock.SetupGet(v => v.OrderPrecedence)
                .Returns(orderPrecedence);

            return mock.Object;
        }
    }
}
