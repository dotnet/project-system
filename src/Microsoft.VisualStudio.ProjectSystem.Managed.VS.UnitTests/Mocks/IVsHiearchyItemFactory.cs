// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IVsHierarchyItemFactory
    {
        public static IVsHierarchyItem Create()
        {
            return Mock.Of<IVsHierarchyItem>();
        }

        public static IVsHierarchyItem ImplementTextProperty(string text = null,
                                                 MockBehavior? mockBehavior = null)
        {
            var behavior = mockBehavior ?? MockBehavior.Default;
            var mock = new Mock<IVsHierarchyItem>(behavior);

            if (text != null)
            {
                mock.Setup(x => x.Text).Returns(text);
            }

            return mock.Object;
        }
    }
}