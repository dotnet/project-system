// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    [Trait("UnitTest", "ProjectSystem")]
    public abstract class AbstractMoveCommandTests
    {
        [Fact]
        public void Constructor_NullAsProjectTree_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => CreateInstance(null, SVsServiceProviderFactory.Create(),
                ConfiguredProjectFactory.Create()));
        }

        [Fact]
        public void Constructor_NullAsSVsServiceProvider_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => CreateInstance(IPhysicalProjectTreeFactory.Create(), null,
                ConfiguredProjectFactory.Create()));
        }

        [Fact]
        public void Constructor_NullAsConfiguredProject_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => CreateInstance(IPhysicalProjectTreeFactory.Create(),
                SVsServiceProviderFactory.Create(), null));
        }

        abstract internal long GetCommandId();

        internal AbstractMoveCommand CreateAbstractInstance(IPhysicalProjectTree projectTree = null, Shell.SVsServiceProvider serviceProvider = null, ConfiguredProject configuredProject = null)
        {
            projectTree = projectTree ?? IPhysicalProjectTreeFactory.Create();
            serviceProvider = serviceProvider ?? SVsServiceProviderFactory.Create();
            configuredProject = configuredProject ?? ConfiguredProjectFactory.Create();

            return CreateInstance(projectTree, serviceProvider, configuredProject);
        }

        internal abstract AbstractMoveCommand CreateInstance(IPhysicalProjectTree projectTree, Shell.SVsServiceProvider serviceProvider, ConfiguredProject configuredProject);
    }
}
