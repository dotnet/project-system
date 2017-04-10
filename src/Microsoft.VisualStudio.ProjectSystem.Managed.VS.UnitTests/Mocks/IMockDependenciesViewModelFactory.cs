// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal class IMockDependenciesViewModelFactory
    {
        public static IDependenciesViewModelFactory Create()
        {
            return Mock.Of<IDependenciesViewModelFactory>();
        }

        public static IDependenciesViewModelFactory Implement(
            ImageMoniker? getDependenciesRootIcon = null,
            IEnumerable<IDependency> createRootViewModel = null,
            IEnumerable<IDependency> createTargetViewModel = null,
            MockBehavior? mockBehavior = null)
        {
            var behavior = mockBehavior ?? MockBehavior.Strict;
            var mock = new Mock<IDependenciesViewModelFactory>(behavior);

            if (getDependenciesRootIcon != null && getDependenciesRootIcon.HasValue)
            {
                mock.Setup(x => x.GetDependenciesRootIcon(It.IsAny<bool>())).Returns(getDependenciesRootIcon.Value);
            }

            if (createRootViewModel != null)
            {
                foreach (var d in createRootViewModel)
                {
                    mock.Setup(x => x.CreateRootViewModel(
                                        It.Is<string>((t) => string.Equals(t, d.ProviderType, System.StringComparison.OrdinalIgnoreCase)),
                                        false))
                        .Returns(d.ToViewModel());
                }
            }

            if (createTargetViewModel != null)
            {
                foreach (var d in createTargetViewModel)
                {
                    mock.Setup(x => x.CreateTargetViewModel(
                            It.Is<ITargetedDependenciesSnapshot>(
                                (t) => string.Equals(t.TargetFramework.Moniker, d.Caption, System.StringComparison.OrdinalIgnoreCase))))
                        .Returns(d.ToViewModel());
                }
            }
            
            return mock.Object;
        }

       
    }
}