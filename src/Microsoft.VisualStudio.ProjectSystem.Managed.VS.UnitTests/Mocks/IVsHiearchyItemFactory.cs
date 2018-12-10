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

        public static IVsHierarchyItem ImplementProperties(string text = null,
                                                           string parentCanonicalName = null,
                                                           MockBehavior mockBehavior = MockBehavior.Default)
        {
            var mock = new Mock<IVsHierarchyItem>(mockBehavior);

            if (text != null)
            {
                mock.Setup(x => x.Text).Returns(text);
            }

            if (parentCanonicalName != null)
            {
                var parentMock = new Mock<IVsHierarchyItem>(mockBehavior);
                parentMock.Setup(x => x.CanonicalName).Returns(parentCanonicalName);
                mock.Setup(x => x.Parent).Returns(parentMock.Object);
            }

            return mock.Object;
        }
    }
}
