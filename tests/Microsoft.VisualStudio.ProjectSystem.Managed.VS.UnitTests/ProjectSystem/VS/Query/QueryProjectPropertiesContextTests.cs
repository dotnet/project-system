// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    public class QueryProjectPropertiesContextTests
    {
        [Theory]
        [MemberData(nameof(ContextsThatAreEqual))]
        public void Equal(IProjectPropertiesContext a, IProjectPropertiesContext b)
        {
            Assert.True(a.Equals(b));
            Assert.True(b.Equals(a));
            Assert.True(a.GetHashCode() == b.GetHashCode());
        }

        [Theory]
        [MemberData(nameof(ContextsThatAreNotEqual))]
        public void NotEqual(IProjectPropertiesContext a, IProjectPropertiesContext b)
        {
            Assert.False(a.Equals(b));
            Assert.False(a.GetHashCode() == b.GetHashCode());
        }

        public static IEnumerable<object[]> ContextsThatAreEqual()
        {
            return new QueryProjectPropertiesContext[][]
            {
                new QueryProjectPropertiesContext[] { QueryProjectPropertiesContext.ProjectFile, QueryProjectPropertiesContext.ProjectFile },
                new QueryProjectPropertiesContext[] { new(true, string.Empty, null, null), QueryProjectPropertiesContext.ProjectFile },
                new QueryProjectPropertiesContext[] { new(true, @"C:\alpha\beta", null, null), new(true, @"c:\ALPHA\Beta", null, null) },
                new QueryProjectPropertiesContext[] { new(true, @"C:\alpha\beta", "myItemType", null), new(true, @"C:\alpha\beta", "MyItemType", null) },
                new QueryProjectPropertiesContext[] { new(true, @"C:\alpha\beta", null, "MyItemName"), new(true, @"C:\alpha\beta", null, "MYITEMNAME") }
            };
        }

        public static IEnumerable<object[]> ContextsThatAreNotEqual()
        {
            return new QueryProjectPropertiesContext[][]
            {
                new QueryProjectPropertiesContext[] { new(false, string.Empty, null, null), QueryProjectPropertiesContext.ProjectFile },
                new QueryProjectPropertiesContext[] { new(true, @"C:\alpha\beta", null, null), new(true, @"C:\alpha\gamma", null, null) },
                new QueryProjectPropertiesContext[] { new(true, @"C:\alpha\beta", "myItemType", null), new(true, @"C:\alpha\beta", "MyOtherItemType", null) },
                new QueryProjectPropertiesContext[] { new(true, @"C:\alpha\beta", null, "MyItemName"), new(true, @"C:\alpha\beta", null, "MyOtherItemName") }
            };
        }
    }
}
