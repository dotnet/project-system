// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [ProjectSystemTrait]
    public class DependencyNodeIdTests
    {
        [Fact]
        public void DependencyNodeId_Constructor()
        {
            Assert.Throws<ArgumentNullException>("providerType", () => {
                var id = new DependencyNodeId(null, null, null, null);
            });

            Assert.Throws<ArgumentException>("providerType", () => {
                var id = new DependencyNodeId("", null, null, null);
            });
        }

        [Theory]
        [InlineData("MyProviderType", "MyItemSpec", "MyItemType", "MyUniqueToken",
                    "file:///[MyProviderType;MyItemSpec;MyItemType;MyUniqueToken]")]
        [InlineData("MyProviderType", "MyItemSpec", "MyItemType", null,
                    "file:///[MyProviderType;MyItemSpec;MyItemType;]")]
        [InlineData("MyProviderType", "MyItemSpec", "", null,
                    "file:///[MyProviderType;MyItemSpec;;]")]
        [InlineData("MyProviderType", null, null, null,
                    "file:///[MyProviderType;;;]")]
        [InlineData("MyProviderType", null, "MyItemType", null,
                    "file:///[MyProviderType;;MyItemType;]")]
        [InlineData("MyProviderType", null, null, "MyUniqueToken",
                    "file:///[MyProviderType;;;MyUniqueToken]")]
        public void DependencyNodeId_ToString(string providerType,
                                                   string itemSpec,
                                                   string itemType,
                                                   string uniqueToken,
                                                   string expectedResult)
        {
            var id = new DependencyNodeId(providerType, itemSpec, itemType, uniqueToken);

            Assert.Equal(expectedResult, id.ToString());
        }

        [Theory]
        [InlineData("file:///[MyProviderType;MyItemSpec;MyItemType;MyUniqueToken]",
                    "MyProviderType", "MyItemSpec", "MyItemType", "MyUniqueToken")]
        [InlineData("",
                    null, null, null, null)]
        [InlineData(null,
                    null, null, null, null)]
        [InlineData("file:///MyProviderType;MyItemSpec;MyItemType;MyUniqueToken",
                    null, null, null, null)]
        [InlineData("MyProviderType;MyItemSpec;MyItemType;MyUniqueToken",
                    null, null, null, null)]
        [InlineData("file:///[MyProviderType;MyItemSpec;MyItemType]",
                    "MyProviderType", "MyItemSpec", "MyItemType", "")]
        [InlineData("file:///[MyProviderType;MyItemSpec]",
                    "MyProviderType", "MyItemSpec", "", "")]
        [InlineData("file:///[MyProviderType]",
                    "MyProviderType", "", "", "")]
        [InlineData("file:///[]",
                    null, null, null, null)]
        public void DependencyNodeId_FromString(string input,
                                                     string expectedProviderType,
                                                     string expectedItemSpec,
                                                     string expectedItemType,
                                                     string expectedUniqueToken)
                                                   
        {

            var id = DependencyNodeId.FromString(input);

            if (expectedProviderType == null)
            {
                // expect null id returned
                Assert.Null(id);
            }
            else
            {
                Assert.Equal(expectedProviderType, id.ProviderType);
                Assert.Equal(expectedItemSpec, id.ItemSpec);
                Assert.Equal(expectedItemType, id.ItemType);
                Assert.Equal(expectedUniqueToken, id.UniqueToken);
            }
        }

        [Theory]
        [InlineData("file:///[MyProviderType;MyItemSpec;MyItemType;MyUniqueToken]",
                    "file:///[myprovidertype;myitemspec;myitemtype;myuniquetoken]",
                    true)]
        [InlineData("file:///[MyProviderType;MyItemSpec;MyItemType;MyUniqueToken]",
                    "file:///[myprovidertype;myitemspec;myitemtype]",
                     false)]
        public void DependencyNodeId_Equals(string firstIdString, 
                                                 string secondIdString, 
                                                 bool expectedResult)
        {
            var id1 = DependencyNodeId.FromString(firstIdString);
            var id2 = DependencyNodeId.FromString(secondIdString);

            Assert.Equal(expectedResult, id1.Equals(id2));
            Assert.Equal(expectedResult, id2.Equals(id1));
        }
    }
}
