// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectChangeDescriptionFactory
    {
        public static IProjectChangeDescription Create()
        {
            return Mock.Of<IProjectChangeDescription>();
        }

        public static IProjectChangeDescription CreateFromSnapshots(IProjectRuleSnapshot before = null, IProjectChangeDiff diff = null, IProjectRuleSnapshot after = null)
        {
            var mock = new Mock<IProjectChangeDescription>();

            if (before != null)
            {
                mock.SetupGet(c => c.Before).Returns(before);
            }

            if (diff != null)
            {
                mock.SetupGet(c => c.Difference).Returns(diff);
            }

            if (after != null)
            {
                mock.SetupGet(c => c.After).Returns(after);
            }

            return mock.Object;
        }
    }
}