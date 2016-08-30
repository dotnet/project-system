// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [ProjectSystemTrait]
    public class DataSourceVersionsSyncTests
    {
        [Fact]
        public void DataSourceVersionsSync_MinimalVersionsPoppingFirst()
        {
            // Arrange
            var id1 = new NamedIdentity("id1");
            var id2 = new NamedIdentity("id2");
            var id3 = new NamedIdentity("id3");

            var ds1 = new Dictionary<NamedIdentity, IComparable>()
            {
                { id1, 1 },
                { id2, 1 },
                { id3, 1 }
            }.ToImmutableDictionary();

            var ds2 = new Dictionary<NamedIdentity, IComparable>()
            {
                { id1, 1 },
                { id3, 2 }
            }.ToImmutableDictionary();

            var ds3 = new Dictionary<NamedIdentity, IComparable>()
            {
                { id2, 2 },
                { id3, 1 }
            }.ToImmutableDictionary();

            var ds4 = new Dictionary<NamedIdentity, IComparable>()
            {
                { id1, 2 },
                { id2, 1 },
                { id3, 1 }
            }.ToImmutableDictionary();

            // Act
            var sync = new DataSourceVersionsSync();
            sync.PushDataSourceVersions(ds1);
            sync.PushDataSourceVersions(ds2);
            sync.PushDataSourceVersions(ds3);
            sync.PushDataSourceVersions(ds4);

            var r1 = sync.PopMinimalDataSourceVersions(ds2);
            var r2 = sync.PopMinimalDataSourceVersions(ds3);
            var r3 = sync.PopMinimalDataSourceVersions(ds1);
            var r4 = sync.PopMinimalDataSourceVersions(ds4);
            var r5 = sync.PopMinimalDataSourceVersions(ds4);
            var r6 = sync.PopMinimalDataSourceVersions(null);

            // Assert
            Assert.True(CompareDictionaries(new Dictionary<NamedIdentity, IComparable>()
            {
                { id1, 1 },
                { id3, 1 }
            }, r1));

            Assert.True(CompareDictionaries(new Dictionary<NamedIdentity, IComparable>()
            {
                { id2, 1 },
                { id3, 1 }
            }, r2));

            Assert.True(CompareDictionaries(new Dictionary<NamedIdentity, IComparable>()
            {
                { id1, 1 },
                { id2, 1 },
                { id3, 1 }
            }, r3));

            Assert.True(CompareDictionaries(new Dictionary<NamedIdentity, IComparable>()
            {
                { id1, 2 },
                { id2, 2 },
                { id3, 2 }
            }, r4));

            Assert.Null(r5);
            Assert.Null(r6);
        }

        private bool CompareDictionaries(IDictionary<NamedIdentity, IComparable> expected,
                                         IImmutableDictionary<NamedIdentity, IComparable> actual)
        {
            if (expected.Count !=  actual.Count)
            {
                return false;
            }

            foreach(var expectedKvp in expected)
            {
                IComparable actualVal = null;
                if (!actual.TryGetValue(expectedKvp.Key, out actualVal))
                {
                    return false;
                }

                if (expectedKvp.Value.CompareTo(actualVal) != 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
