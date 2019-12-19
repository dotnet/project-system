// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation.VisualBasic
{
    public class VisualBasicNamespaceImportsListTests
    {
        [Fact]
        public void Constructor_NotNull()
        {
            var list = new VisualBasicNamespaceImportsList(Mock.Of<IActiveConfiguredProjectSubscriptionService>());

            Assert.NotNull(list);
        }

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

        [Fact]
        public void UpdateNamespaceImportListTest()
        {
            IEnumerable<(string[] names, string[] expected)> updates = new[]
            {
                (new [] { "A", "B", "C", "D" },      new [] { "A", "B", "C", "D" }),      // Initial add
                (new [] { "A", "B", "C" },           new [] { "A", "B", "C" }),           // Remove from the end
                (new [] { "B", "C" },                new [] { "B", "C" }),                // Remove from the beginning
                (new [] { "A", "B", "C" },           new [] { "A", "B", "C" }),           // Add at the beginning
                (new [] { "A", "B", "C", "E" },      new [] { "A", "B", "C", "E" }),      // Add at the end
                (new [] { "A", "B", "C", "D", "E" }, new [] { "A", "B", "C", "D", "E" }), // Add in the middle
                (new [] { "A", "B", "D", "E" },      new [] { "A", "B", "D", "E" }),      // Remove from the middle
                (new [] { "F", "C", "B", "E" },      new [] { "B", "C", "E", "F" }),      // Addition and deletion in jumbled order with the same no of elements as before
            };

            var list = VisualBasicNamespaceImportsListFactory.CreateInstance();

            foreach (var (names, expected) in updates)
            {
                var json = ConstructNamespaceImportChangeJson(names);
                var projectSubscriptionUpdate = IProjectSubscriptionUpdateFactory.FromJson(json);
                var projectVersionedValue = IProjectVersionedValueFactory.Create(projectSubscriptionUpdate);

                list.OnNamespaceImportChanged(projectVersionedValue);

                AssertEx.SequenceEqual(expected, list);
            }

            return;

            static string ConstructNamespaceImportChangeJson(string[] importNames)
            {
                var json = @"{
    ""ProjectChanges"": {
        ""NamespaceImport"": {
            ""Difference"": {
                ""AnyChanges"": ""True""
            },
            ""After"": {
                ""Items"": {";

                for (int i = 0; i < importNames.Length; i++)
                {
                    json += @"                   """ + importNames[i] + @""" : {}";
                    if (i != importNames.Length - 1)
                    {
                        json += ",";
                    }
                }

                json += @"                }
               }
           }
       }
   }";
                return json;
            }
        }
    }
}
