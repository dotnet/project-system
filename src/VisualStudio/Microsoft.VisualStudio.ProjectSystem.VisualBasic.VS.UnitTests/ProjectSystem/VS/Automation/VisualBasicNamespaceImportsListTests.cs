// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    [ProjectSystemTrait]
    public class VisualBasicNamespaceImportsListTests
    {
        [Fact]
        public void Constructor_NullAsActiveConfiguredProjectSubscriptionServices_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("activeConfiguredProjectSubscriptionService", () =>
            {
                new VisualBasicNamespaceImportsList((IActiveConfiguredProjectSubscriptionService)null);
            });
        }

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
            Assert.Equal(list.Count, 2);

            //GetEnumerator
            var enumerator = list.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.True(string.Compare(enumerator.Current, "A") == 0);
            Assert.True(enumerator.MoveNext());
            Assert.True(string.Compare(enumerator.Current, "B") == 0);
            Assert.False(enumerator.MoveNext());

            //IsPresent(string)
            Assert.Throws<ArgumentException>("bstrImport", () =>
            {
                list.IsPresent(null);
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
            Assert.True(string.Compare(list.Item(1), "A") == 0);
            Assert.True(string.Compare(list.Item(2), "B") == 0);
        }

        [Fact]
        public void UpdateNamespaceImportListTest()
        {
            var list = VisualBasicNamespaceImportsListFactory.CreateInstance();
            var dataList = new List<string>();
            list.SetList(dataList);

            // Initial add
            list.OnNamespaceImportChanged(
                IProjectVersionedValueFactory<IProjectSubscriptionUpdate>.Create(GetProjectSubscriptionUpdate("A", "B", "C", "D")));
            VerifyList(dataList, "A", "B", "C", "D");

            // Remove from the end
            list.OnNamespaceImportChanged(
                IProjectVersionedValueFactory<IProjectSubscriptionUpdate>.Create(GetProjectSubscriptionUpdate("A", "B", "C")));
            VerifyList(dataList, "A", "B", "C");

            // Remove from the beginning
            list.OnNamespaceImportChanged(
                IProjectVersionedValueFactory<IProjectSubscriptionUpdate>.Create(GetProjectSubscriptionUpdate("B", "C")));
            VerifyList(dataList, "B", "C");

            // Add at the beginning
            list.OnNamespaceImportChanged(
                IProjectVersionedValueFactory<IProjectSubscriptionUpdate>.Create(GetProjectSubscriptionUpdate("A", "B", "C")));
            VerifyList(dataList, "A", "B", "C");

            // Add at the end
            list.OnNamespaceImportChanged(
                IProjectVersionedValueFactory<IProjectSubscriptionUpdate>.Create(GetProjectSubscriptionUpdate("A", "B", "C", "E")));
            VerifyList(dataList, "A", "B", "C", "E");

            // Add in the middle
            list.OnNamespaceImportChanged(
                IProjectVersionedValueFactory<IProjectSubscriptionUpdate>.Create(GetProjectSubscriptionUpdate("A", "B", "C", "D", "E")));
            VerifyList(dataList, "A", "B", "C", "D", "E");

            // Remove from the middle
            list.OnNamespaceImportChanged(
                IProjectVersionedValueFactory<IProjectSubscriptionUpdate>.Create(GetProjectSubscriptionUpdate("A", "B", "D", "E")));
            VerifyList(dataList, "A", "B", "D", "E");

            // Addition and Deletion in jumbled order with the same no of elements as before
            list.OnNamespaceImportChanged(
                IProjectVersionedValueFactory<IProjectSubscriptionUpdate>.Create(GetProjectSubscriptionUpdate("F", "C", "B", "E")));
            VerifyList(dataList, "B", "C", "E", "F");
        }

        private static IProjectSubscriptionUpdate GetProjectSubscriptionUpdate(params string[] importNames)
        {
            return IProjectSubscriptionUpdateFactory.FromJson(ConstructNamespaceImportChangeJson(importNames));
        }

        private static string ConstructNamespaceImportChangeJson(string[] importNames)
        {
            var json = @"{
    ""ProjectChanges"": {
        ""NamespaceImport"": {
            ""Difference"": {
                ""AnyChanges"": ""True""
            },
            ""After"": {
                ""Items"": {";

            for (int i = 0; i < importNames.Length ; i++)
            {
                json += @"                   """ + importNames[i] + @""" : {}";
                if (i != importNames.Length - 1)
                {
                    json += ",";
                }
            }

            json = json + @"                }
                                        }
                                    }
                                }
                            }";
            return json;
        }

        private static void VerifyList(List<string> list, params string[] expected)
        {
            Assert.Equal(list.Count, expected.Count());
            for (int i = 0; i < list.Count ; i++)
            {
                Assert.True(string.Compare(list[i], expected[i]) == 0);
            }
        }
    }
}
