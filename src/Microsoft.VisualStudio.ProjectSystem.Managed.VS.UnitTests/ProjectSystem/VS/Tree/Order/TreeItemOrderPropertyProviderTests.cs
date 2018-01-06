// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Order
{
    [Trait("UnitTest", "ProjectSystem")]
    public class TreeItemOrderPropertyProviderTests
    {
        private List<(string type, string include)> _simpleOrderFile = new List<(string type, string include)>
        {
            ("Compile", "Order.fs"),
            ("Compile", "Customer.fs"),
            ("Compile", "Program.fs")
        };

        [Theory]
        [InlineData("Customer.fs", "Compile", false, 2)]
        [InlineData("order.fs", "Compile", false, 1)] // case insensitive
        [InlineData("Program.fs", "Compile", false, 3)] 
        [InlineData("Misc.txt", "Content", false, int.MaxValue)] // unknown type
        [InlineData("ordered.fsproj", null, false, int.MaxValue)] // hidden file
        [InlineData("Debug", null, true, 0)] // unknown folder
        public void VerifySimpleOrderedUnderProjectRoot(string itemName, string itemType, bool isFolder, int expectedOrder)
        {
            var orderedItems = _simpleOrderFile
                .Select(p => new ProjectItemIdentity(p.type, p.include))
                .ToList();

            var provider = new TreeItemOrderPropertyProvider(orderedItems, UnconfiguredProjectFactory.Create());

            var context = GetContext(itemName, itemType, isFolder, ProjectTreeFlags.ProjectRoot);
            var values = GetInitialValues();

            provider.CalculatePropertyValues(context, values);

            Assert.Equal(expectedOrder, values.DisplayOrder);
        }

        [Theory]
        [MemberData(nameof(TestTreeItems))]
        public void VerifyOrderIncreasesMonotonically(
            List<(string type, string include)> orderedFileInput,
            List<(string itemName, string itemType, bool isFolder, bool isUnderProjectRoot)> solutionTree)
        {
            var orderedItems = orderedFileInput
                .Select(p => new ProjectItemIdentity(p.type, p.include))
                .ToList();

            var provider = new TreeItemOrderPropertyProvider(orderedItems, UnconfiguredProjectFactory.Create());

            var lastOrder = 0;
            solutionTree.ForEach(item =>
            {
                var context = GetContext(item.itemName, item.itemType, item.isFolder, 
                    item.isUnderProjectRoot ? ProjectTreeFlags.ProjectRoot : ProjectTreeFlags.Empty);
                var values = GetInitialValues();

                provider.CalculatePropertyValues(context, values);

                Assert.True(values.DisplayOrder >= lastOrder);
                lastOrder = values.DisplayOrder;
            });

            Assert.True(lastOrder >= orderedFileInput.Count);
        }

        public static IEnumerable<object[]> TestTreeItems
        {
            get
            {
                return new[]
                {
                    // 1. simple ordering with no folders in evaluated include
                    new object[]
                    {
                        new List<(string type, string include)>
                        {
                            ("Compile", "Order.fs"),
                            ("Compile", "Customer.fs"),
                            ("Compile", "Program.fs")
                        },
                        new List<(string itemName, string itemType, bool isFolder, bool isUnderProjectRoot)>
                        {
                            // unknown folders and their nested items
                            ("Debug", null, true, true),
                            ("bin", null, true, false),

                            // included items
                            ("Order.fs", "Compile", false, true),
                            ("Customer.fs", "Compile", false, true),
                            ("Program.fs", "Compile", false, true),

                            // hidden or other items under project root
                            ("profile.png", "Content", false, true),
                            ("app.fsproj", null, false, true),
                            ("app.sln", null, false, true)
                        }
                    },

                    // 2. nested ordering with folders that should also be ordered
                    new object[]
                    {
                        new List<(string type, string include)>
                        {
                            ("Compile", "Order/Order.fs"),
                            ("Compile", "Customer/Postcode.fs"),
                            ("Compile", "Customer\\Customer.fs"),
                            ("Compile", "Customer/Telemetry/Data.fs"),
                            ("Compile", "Customer/Address.fs"),
                            ("Content", "Bio.png"),
                            ("Compile", "Program.fs"),
                        },
                        new List<(string itemName, string itemType, bool isFolder, bool isUnderProjectRoot)>
                        {
                            // unknown folders 
                            (".vs", "Folder", true, false),
                            ("bin", "Folder", true, true),
                            ("netcoreapp2.0", "Folder", true, false),
                            ("obj", "Folder", true, true),

                            // included items
                            ("Order", "Folder", true, true),
                                ("Order.fs", "Compile", false, false),
                            ("Customer", "Folder", true, true),
                                ("Postcode.fs", "Compile", false, false),
                                ("Customer.fs", "Compile", false, false),
                                ("Telemetry", "Folder", true, false),
                                    ("Data.fs", "Compile", false, false),
                                ("Address.fs", "Compile", false, false),
                            ("Bio.png", "Content", false, true),
                            ("Program.fs", "Compile", false, true),

                            // hidden or other items under project root
                            ("app.fsproj", null, false, true),
                            ("app.sln", null, false, true)
                        }
                    }
                };
            }
        }

        private List<(string type, string include)> _orderedWithDups = new List<(string type, string include)>
        {
            ("Compile", "Common.fs"),
            ("Compile", "Tables\\Order.fs"),
            ("Compile", "Tables\\Common.fs"),
            ("Compile", "Program.fs")
        };

        [Theory]
        [InlineData("Common.fs",  "Compile", false, "X:\\Project\\Common.fs", 1)]
        [InlineData("Tables",     "Folder",  true,  null, 2)]
        [InlineData("Order.fs",   "Compile", false, "X:\\Project\\Tables\\Order.fs", 3)]
        [InlineData("Common.fs",  "Compile", false, "X:\\Project\\Tables\\Common.fs", 4)] // duplicate and out of alphabetical order
        [InlineData("Program.fs", "Compile", false, "X:\\Project\\Program.fs", 5)]
        public void VerifyOrderingWithDuplicateFiles(string itemName, string itemType, bool isFolder, string rootedPath, int expectedOrder)
        {
            var orderedItems = _orderedWithDups
                .Select(p => new ProjectItemIdentity(p.type, p.include))
                .ToList();

            var provider = new TreeItemOrderPropertyProvider(orderedItems, UnconfiguredProjectFactory.Create(filePath: "X:\\Project\\"));

            bool isUnderProjectRoot = rootedPath?.Contains("Tables") != true;
            var metadata = rootedPath == null ? null : new Dictionary<string, string>{{"FullPath", rootedPath}}.ToImmutableDictionary();

            var context = GetContext(itemName, itemType, isFolder, 
                isUnderProjectRoot ? ProjectTreeFlags.ProjectRoot : ProjectTreeFlags.Empty, 
                metadata);
            var values = GetInitialValues();

            provider.CalculatePropertyValues(context, values);

            Assert.Equal(expectedOrder, values.DisplayOrder);
        }

        private static IProjectTreeCustomizablePropertyContext GetContext(
            string itemName = null,
            string itemType = null,
            bool isFolder = false,
            ProjectTreeFlags flags = default(ProjectTreeFlags),
            IImmutableDictionary<string, string> metadata = null)
            => IProjectTreeCustomizablePropertyContextFactory.Implement(
                itemName: itemName, 
                itemType: itemType, 
                isFolder: isFolder, 
                flags: flags, 
                metadata: metadata);

        private static ProjectTreeCustomizablePropertyValues GetInitialValues() =>
            new ProjectTreeCustomizablePropertyValues { DisplayOrder = 0 };

        private class ProjectTreeCustomizablePropertyValues : 
            IProjectTreeCustomizablePropertyValues, 
            IProjectTreeCustomizablePropertyValues2
        {
            public ProjectTreeFlags Flags { get; set; }

            public ProjectImageMoniker Icon { get; set; }

            public ProjectImageMoniker ExpandedIcon { get; set; }

            public int DisplayOrder { get; set; }
        }
    }
}
