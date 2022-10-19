// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.References;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Order
{
    public class TreeItemOrderPropertyProviderTests
    {
        private readonly List<(string type, string include)> _simpleOrderFile = new()
        {
            ("Compile", "Order.fs"),
            ("Compile", "Customer.fs"),
            ("Compile", "Program.fs")
        };

        [Theory]
        [InlineData("Customer.fs", "Compile", false, "X:\\Project\\Customer.fs", 2)]
        [InlineData("order.fs", "Compile", false, "X:\\Project\\order.fs", 1)] // case insensitive
        [InlineData("Program.fs", "Compile", false, "X:\\Project\\Program.fs", 3)]
        [InlineData("Misc.txt", "Content", false, "X:\\Project\\Misc.txt", int.MaxValue)] // unknown type
        [InlineData("ordered.fsproj", null, false, "X:\\Project\\ordered.fsproj", int.MaxValue)] // hidden file
        [InlineData("Debug", null, true, null, 0)] // unknown folder
        public void VerifySimpleOrderedUnderProjectRoot(string itemName, string? itemType, bool isFolder, string? rootedPath, int expectedOrder)
        {
            var orderedItems = _simpleOrderFile
                .Select(p => new ProjectItemIdentity(p.type, p.include))
                .ToList();

            var provider = new TreeItemOrderPropertyProvider(orderedItems, UnconfiguredProjectFactory.Create(fullPath: "X:\\Project\\"));

            var metadata = rootedPath is null ? null : new Dictionary<string, string> { { "FullPath", rootedPath } }.ToImmutableDictionary();

            var context = GetContext(itemName, itemType, isFolder, ProjectTreeFlags.ProjectRoot, metadata);
            var values = GetInitialValues();

            provider.CalculatePropertyValues(context, values);

            Assert.Equal(expectedOrder, values.DisplayOrder);
        }

        private readonly List<(string type, string include)> _simpleOrderFileDuplicate = new()
        {
            ("Compile", "Order.fs"),
            ("Compile", "Customer.fs"),
            ("Compile", "Program.fs"),
            ("Compile", "Program.fs")
        };

        [Theory]
        [InlineData("Customer.fs", "Compile", false, "X:\\Project\\Customer.fs", 2)]
        [InlineData("order.fs", "Compile", false, "X:\\Project\\order.fs", 1)] // case insensitive
        [InlineData("Program.fs", "Compile", false, "X:\\Project\\Program.fs", 3)]
        [InlineData("Misc.txt", "Content", false, "X:\\Project\\Misc.txt", int.MaxValue)] // unknown type
        [InlineData("ordered.fsproj", null, false, "X:\\Project\\ordered.fsproj", int.MaxValue)] // hidden file
        [InlineData("Debug", null, true, null, 0)] // unknown folder
        public void VerifySimpleOrderedUnderProjectRootDuplicate(string itemName, string? itemType, bool isFolder, string? rootedPath, int expectedOrder)
        {
            var orderedItems = _simpleOrderFileDuplicate
                .Select(p => new ProjectItemIdentity(p.type, p.include))
                .ToList();

            var provider = new TreeItemOrderPropertyProvider(orderedItems, UnconfiguredProjectFactory.Create(fullPath: "X:\\Project\\"));

            var metadata = rootedPath is null ? null : new Dictionary<string, string> { { "FullPath", rootedPath } }.ToImmutableDictionary();

            var context = GetContext(itemName, itemType, isFolder, ProjectTreeFlags.ProjectRoot, metadata);
            var values = GetInitialValues();

            provider.CalculatePropertyValues(context, values);

            Assert.Equal(expectedOrder, values.DisplayOrder);
        }

        [Theory]
        [MemberData(nameof(TestTreeItems))]
        public void VerifyOrderIncreasesMonotonically(
            List<(string type, string include)> orderedFileInput,
            List<(string itemName, string? itemType, bool isFolder, bool isUnderProjectRoot, string? rootedPath)> solutionTree)
        {
            var orderedItems = orderedFileInput
                .Select(p => new ProjectItemIdentity(p.type, p.include))
                .ToList();

            var provider = new TreeItemOrderPropertyProvider(orderedItems, UnconfiguredProjectFactory.Create(fullPath: "X:\\Project\\"));

            var lastOrder = 0;
            solutionTree.ForEach(item =>
            {
                var rootedPath = item.rootedPath;
                var metadata = rootedPath is null ? null : new Dictionary<string, string> { { "FullPath", rootedPath } }.ToImmutableDictionary();

                var context = GetContext(item.itemName, item.itemType, item.isFolder,
                    item.isUnderProjectRoot ? ProjectTreeFlags.ProjectRoot : ProjectTreeFlags.Empty, metadata);
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
                        new List<(string itemName, string? itemType, bool isFolder, bool isUnderProjectRoot, string? rootedPath)>
                        {
                            // unknown folders and their nested items
                            ("Debug", null, true, true, "X:\\Project\\Debug"),
                            ("bin", null, true, false, "X:\\Project\\bin"),

                            // included items
                            ("Order.fs", "Compile", false, true, "X:\\Project\\Order.fs"),
                            ("Customer.fs", "Compile", false, true, "X:\\Project\\Customer.fs"),
                            ("Program.fs", "Compile", false, true, "X:\\Project\\Program.fs"),

                            // hidden or other items under project root
                            ("profile.png", "Content", false, true, "X:\\Project\\profile.png"),
                            ("app.fsproj", null, false, true, "X:\\Project\\app.fsproj"),
                            ("app.sln", null, false, true, "X:\\Project\\app.sln")
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
                        new List<(string itemName, string? itemType, bool isFolder, bool isUnderProjectRoot, string? rootedPath)>
                        {
                            // rootedPath is set to null for folders as we never get any metadata for folders in reality.

                            // unknown folders 
                            (".vs", "Folder", true, false, null),
                            ("bin", "Folder", true, true, null),
                            ("netcoreapp2.0", "Folder", true, false, null),
                            ("obj", "Folder", true, true, null),

                            // included items
                            ("Order", "Folder", true, true, null),
                                ("Order.fs", "Compile", false, false, "X:\\Project\\Order\\Order.fs"),
                            ("Customer", "Folder", true, true, null),
                                ("Postcode.fs", "Compile", false, false, "X:\\Project\\Customer\\Postcode.fs"),
                                ("Customer.fs", "Compile", false, false, "X:\\Project\\Customer\\Customer.fs"),
                                ("Telemetry", "Folder", true, false, null),
                                    ("Data.fs", "Compile", false, false, "X:\\Project\\Customer\\Telemetry\\Data.fs"),
                                ("Address.fs", "Compile", false, false, "X:\\Project\\Customer\\Address.fs"),
                            ("Bio.png", "Content", false, true, "X:\\Project\\Bio.png"),
                            ("Program.fs", "Compile", false, true, "X:\\Project\\Program.fs"),

                            // hidden or other items under project root
                            ("app.fsproj", null, false, true, "X:\\Project\\app.fsproj"),
                            ("app.sln", null, false, true, "X:\\Project\\app.sln")
                        }
                    }
                };
            }
        }

        private readonly List<(string type, string include)> _orderedWithDups = new()
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
        public void VerifyOrderingWithDuplicateFiles(string itemName, string itemType, bool isFolder, string? rootedPath, int expectedOrder)
        {
            var orderedItems = _orderedWithDups
                .Select(p => new ProjectItemIdentity(p.type, p.include))
                .ToList();

            var provider = new TreeItemOrderPropertyProvider(orderedItems, UnconfiguredProjectFactory.Create(fullPath: "X:\\Project\\"));

            bool isUnderProjectRoot = rootedPath?.Contains("Tables") != true;
            var metadata = rootedPath is null ? null : new Dictionary<string, string> { { "FullPath", rootedPath } }.ToImmutableDictionary();

            var context = GetContext(itemName, itemType, isFolder,
                isUnderProjectRoot ? ProjectTreeFlags.ProjectRoot : ProjectTreeFlags.Empty,
                metadata);
            var values = GetInitialValues();

            provider.CalculatePropertyValues(context, values);

            Assert.Equal(expectedOrder, values.DisplayOrder);
        }

        private readonly List<(string type, string include, string? linkPath)> _orderedWithLinkPaths = new()
        {
            ("Compile", "Common.fs",         "Tables/Test.fs"),
            ("Compile", "Tables\\Order.fs",  null),
            ("Compile", "Tables\\Common.fs", null),
            ("Compile", "Program.fs",        null)
        };

        [Theory]
        [InlineData("Common.fs", "Compile", false, "X:\\Project\\Common.fs", "Tables/Test.fs", 2)] // Our link path
        [InlineData("Tables", "Folder", true, null, null, 1)]
        [InlineData("Order.fs", "Compile", false, "X:\\Project\\Tables\\Order.fs", null, 3)]
        [InlineData("Common.fs", "Compile", false, "X:\\Project\\Tables\\Common.fs", null, 4)] // duplicate and out of alphabetical order
        [InlineData("Program.fs", "Compile", false, "X:\\Project\\Program.fs", null, 5)]
        public void VerifyOrderingWithLinkPaths(string itemName, string itemType, bool isFolder, string? rootedPath, string? linkPath, int expectedOrder)
        {
            var orderedItems = _orderedWithLinkPaths
                .Select(p => new ProjectItemIdentity(p.type, p.include, p.linkPath))
                .ToList();

            var provider = new TreeItemOrderPropertyProvider(orderedItems, UnconfiguredProjectFactory.Create(fullPath: "X:\\Project\\"));

            bool isUnderProjectRoot = rootedPath?.Contains("Tables") != true;
            var metadata = rootedPath is null ? null : new Dictionary<string, string> { { "FullPath", rootedPath }, { "Link", linkPath ?? "" } }.ToImmutableDictionary();

            var context = GetContext(itemName, itemType, isFolder,
                isUnderProjectRoot ? ProjectTreeFlags.ProjectRoot : ProjectTreeFlags.Empty,
                metadata);
            var values = GetInitialValues();

            provider.CalculatePropertyValues(context, values);

            Assert.Equal(expectedOrder, values.DisplayOrder);
        }

        private static IProjectTreeCustomizablePropertyContext GetContext(
            string? itemName = null,
            string? itemType = null,
            bool isFolder = false,
            ProjectTreeFlags flags = default,
            IImmutableDictionary<string, string>? metadata = null)
            => IProjectTreeCustomizablePropertyContextFactory.Implement(
                itemName: itemName,
                itemType: itemType,
                isFolder: isFolder,
                flags: flags,
                metadata: metadata);

        private static ReferencesProjectTreeCustomizablePropertyValues GetInitialValues() =>
            new();
    }
}
