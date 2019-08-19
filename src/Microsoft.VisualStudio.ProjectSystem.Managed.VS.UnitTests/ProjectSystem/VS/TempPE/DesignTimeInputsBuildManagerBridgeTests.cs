// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.VS.Automation;

using Moq;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    public class DesignTimeInputsBuildManagerBridgeTests
    {
        [Fact]
        public async Task ChangedFile_FiresTempPEDirty()
        {
            var mgr = new TestBuildManager();
            using var bridge = new TestDesignTimeInputsBuildManagerBridge(mgr);

            await bridge.TestProcessAsync(new DesignTimeInputsDelta(
                ImmutableHashSet.CreateRange(new string[] { "Resources1.Designer.cs" }),
                ImmutableHashSet<string>.Empty,
                new DesignTimeInputFileChange[] { new DesignTimeInputFileChange("Resources1.Designer.cs", false) },
                ""));

            // One file should have been added
            Assert.Single(mgr.DirtyItems);
            Assert.Equal("Resources1.Designer.cs", mgr.DirtyItems.First());
            Assert.Empty(mgr.DeletedItems);
        }

        [Fact]
        public async Task RemovedFile_FiresTempPEDeleted()
        {
            var mgr = new TestBuildManager();
            using var bridge = new TestDesignTimeInputsBuildManagerBridge(mgr);

            await bridge.TestProcessAsync(new DesignTimeInputsDelta(
                ImmutableHashSet.CreateRange(new string[] { "Resources1.Designer.cs" }),
                ImmutableHashSet<string>.Empty,
                Array.Empty<DesignTimeInputFileChange>(),
                ""));

            await bridge.TestProcessAsync(new DesignTimeInputsDelta(
               ImmutableHashSet<string>.Empty,
               ImmutableHashSet<string>.Empty,
               Array.Empty<DesignTimeInputFileChange>(),
               ""));

            // One file should have been added
            Assert.Empty(mgr.DirtyItems);
            Assert.Single(mgr.DeletedItems);
            Assert.Equal("Resources1.Designer.cs", mgr.DeletedItems.First());
        }

        internal class TestDesignTimeInputsBuildManagerBridge : DesignTimeInputsBuildManagerBridge
        {
            public TestDesignTimeInputsBuildManagerBridge(VSBuildManager buildManager)
                : base(IProjectThreadingServiceFactory.Create(),
                       IUnconfiguredProjectCommonServicesFactory.Create(threadingService: IProjectThreadingServiceFactory.Create()),
                       Mock.Of<IDesignTimeInputsChangeTracker>(),
                       Mock.Of<IDesignTimeInputsCompiler>(),
                       buildManager)
            {
            }

            public Task TestProcessAsync(DesignTimeInputsDelta delta)
            {
                var input = IProjectVersionedValueFactory.Create(delta);

                return base.ApplyAsync(input);
            }
        }

        internal class TestBuildManager : VSBuildManager
        {
            public List<string> DeletedItems { get; } = new List<string>();
            public List<string> DirtyItems { get; } = new List<string>();

            internal TestBuildManager()
                : base(IProjectThreadingServiceFactory.Create(),
                       IUnconfiguredProjectCommonServicesFactory.Create(UnconfiguredProjectFactory.Create()))
            {
            }

            internal override void OnDesignTimeOutputDeleted(string outputMoniker)
            {
                DeletedItems.Add(outputMoniker);
            }

            internal override void OnDesignTimeOutputDirty(string outputMoniker)
            {
                DirtyItems.Add(outputMoniker);
            }

        }
    }
}
