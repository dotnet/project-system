// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [ProjectSystemTrait]
    public class TargetedDependenciesSnapshotTests
    {
        [Fact]
        public void TargetedDependenciesSnapshot_Constructor_WhenRequiredParamsNotProvided_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>("projectPath", () =>
            {
                new TestableTargetedDependenciesSnapshot(null, null);
            });

            Assert.Throws<ArgumentNullException>("targetFramework", () =>
            {
                new TestableTargetedDependenciesSnapshot("someprojectpath", null);
            });
        }

        [Fact]
        public void TargetedDependenciesSnapshot_Constructor()
        {
           
        }

        private class TestableTargetedDependenciesSnapshot : TargetedDependenciesSnapshot
        {
            public TestableTargetedDependenciesSnapshot(
                string projectPath,
                ITargetFramework targetFramework,
                ITargetedDependenciesSnapshot previousSnapshot = null,
                IProjectCatalogSnapshot catalogs = null)
                : base(projectPath, targetFramework, previousSnapshot, catalogs)
            {
            }
        }

    }
}
