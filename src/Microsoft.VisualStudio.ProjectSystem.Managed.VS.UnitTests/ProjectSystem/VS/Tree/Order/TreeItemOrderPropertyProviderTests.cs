// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Order
{
    [ProjectSystemTrait]
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
        [InlineData("Misc.txt", "Content", false, 4)] // unknown type
        [InlineData("ordered.fsproj", null, false, 4)] // hidden file
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

            Assert.Equal(values.DisplayOrder, expectedOrder);
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
                var context = GetContext(item.itemName, item.itemType, item.isFolder, item.isUnderProjectRoot ? ProjectTreeFlags.ProjectRoot : ProjectTreeFlags.Empty);
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
                            ("app.exe", null, false, false),

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
                            // unknown folders and their nested items
                            (".vs", "Folder", true, false),
                            ("bin", "Folder", true, true),
                            (".suo", null, false, false),
                            ("netcoreapp2.0", "Folder", true, false),
                            ("obj", "Folder", true, true),
                            (".suo", null, false, false),
                            ("app.fsproj.nuget.g.props", null, false, false),
                            ("app.fsproj.nuget.g.targets", null, false, false),

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

        private static IProjectTreeCustomizablePropertyContext GetContext(
            string itemName = null,
            string itemType = null,
            bool isFolder = false,
            ProjectTreeFlags flags = default(ProjectTreeFlags))
            => IProjectTreeCustomizablePropertyContextFactory.Implement(itemName, itemType, isFolder, flags);

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
