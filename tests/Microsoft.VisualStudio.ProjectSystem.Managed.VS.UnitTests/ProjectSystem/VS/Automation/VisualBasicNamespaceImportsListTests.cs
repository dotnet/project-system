// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    public class VisualBasicNamespaceImportsListTests
    {
        [Fact]
        public void UnderlyingListBasedPropertiesTest()
        {
            var list = VisualBasicNamespaceImportsListFactory.CreateInstance("A", "B");

            //Count
            Assert.Equal(2, list.Count);

            //GetEnumerator
            var enumerator = list.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal("A", enumerator.Current);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("B", enumerator.Current);
            Assert.False(enumerator.MoveNext());

            //IsPresent(string)
            Assert.Throws<ArgumentException>("bstrImport", () =>
            {
                list.IsPresent(null!);
            });
            Assert.Throws<ArgumentException>("bstrImport", () =>
            {
                list.IsPresent("");
            });
            Assert.True(list.IsPresent("A"));
            Assert.False(list.IsPresent("C"));

            //IsPresent(int)
            Assert.Throws<ArgumentException>("indexInt", () =>
            {
                list.IsPresent(0);
            });
            Assert.Throws<ArgumentException>("indexInt", () =>
            {
                list.IsPresent(3);
            });
            Assert.True(list.IsPresent(1));
            Assert.True(list.IsPresent(2));

            //Item(int)
            Assert.Throws<ArgumentException>("lIndex", () =>
            {
                list.Item(0);
            });
            Assert.Throws<ArgumentException>("lIndex", () =>
            {
                list.Item(3);
            });
            Assert.Equal("A", list.Item(1));
            Assert.Equal("B", list.Item(2));
        }

        [Theory]
        [InlineData(new string[0],                       new [] { "A", "B", "C", "D" }      , new [] { "A", "B", "C", "D" }, new string[0]       )] // Initial add
        [InlineData(new [] { "A", "B", "C", "D" },       new [] { "A", "B", "C" }           , new string[0],                 new [] { "D" }      )] // Remove from the end
        [InlineData(new [] { "A", "B", "C" },            new [] { "B", "C" }                , new string[0],                 new [] { "A" }      )] // Remove from the beginning
        [InlineData(new [] { "B", "C" },                 new [] { "A", "B", "C" }           , new [] { "A" },                new string[0]       )] // Add at the beginning
        [InlineData(new [] { "A", "B", "C" },            new [] { "A", "B", "C", "E" }      , new [] { "E" },                new string[0]       )] // Add at the end
        [InlineData(new [] { "A", "B", "C", "E"},        new [] { "A", "B", "C", "D", "E" } , new [] { "D" },                new string[0]       )] // Add in the middle
        [InlineData(new [] { "A", "B", "C", "D", "E" },  new [] { "A", "B", "D", "E" }      , new string[0],                 new [] { "C" }      )] // Remove from the middle
        [InlineData(new [] { "A", "B", "D", "E" },       new [] { "B", "C", "E", "F" }      , new [] { "C", "F" },           new [] { "A", "D" } )] // Addition and deletion in jumbled order with the same no of elements as before

        public void UpdateNamespaceImportListTest(string[] initialState, string[] updateToApply, string[] expectedAdded, string[] expectedRemoved)
        {
            var list = VisualBasicNamespaceImportsListFactory.CreateInstance(initialState);

            var json = ConstructNamespaceImportChangeJson(updateToApply);
            var projectSubscriptionUpdate = IProjectSubscriptionUpdateFactory.FromJson(json);

            list.TestApply(projectSubscriptionUpdate);

            // Updates represent the final state, so they are the expected list too
            Assert.Equal(updateToApply.OrderBy(s=>s), list.OrderBy(s=>s));
            Assert.Equal(expectedAdded.OrderBy(s=>s), list.ImportsAdded.OrderBy(s=>s));
            Assert.Equal(expectedRemoved.OrderBy(s=>s), list.ImportsRemoved.OrderBy(s=>s));

            return;

            static string ConstructNamespaceImportChangeJson(string[] importNames)
            {
                var json =
                    """
                    {
                        "ProjectChanges": {
                            "NamespaceImport": {
                                "Difference": {
                                    "AnyChanges": "True"
                                },
                                "After": {
                                    "Items": {
                    """;

                for (int i = 0; i < importNames.Length; i++)
                {
                    json += @"                   """ + importNames[i] + @""" : {}";
                    if (i != importNames.Length - 1)
                    {
                        json += ",";
                    }
                }

                json +=
                    """
                                   }
                                }
                            }
                        }
                    }
                    """;
                return json;
            }
        }
    }
}
