// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IMockDependenciesViewModelFactory
    {
        public static IDependenciesViewModelFactory Implement(
            ImageMoniker? getDependenciesRootIcon = null,
            IEnumerable<IDependencyModel>? createRootViewModel = null,
            IEnumerable<IDependencyModel>? createTargetViewModel = null,
            MockBehavior mockBehavior = MockBehavior.Strict)
        {
            var mock = new Mock<IDependenciesViewModelFactory>(mockBehavior);

            if (getDependenciesRootIcon.HasValue)
            {
                mock.Setup(x => x.GetDependenciesRootIcon(It.IsAny<DiagnosticLevel>())).Returns(getDependenciesRootIcon.Value);
            }

            if (createRootViewModel is not null)
            {
                foreach (var d in createRootViewModel)
                {
                    mock.Setup(x => x.CreateGroupNodeViewModel(
                            It.Is<string>(t => string.Equals(t, d.ProviderType, StringComparison.OrdinalIgnoreCase)),
                            DiagnosticLevel.None))
                        .Returns((d.ToViewModel(DiagnosticLevel.None), null));
                    mock.Setup(x => x.CreateGroupNodeViewModel(
                            It.Is<string>(t => string.Equals(t, d.ProviderType, StringComparison.OrdinalIgnoreCase)),
                            DiagnosticLevel.Warning))
                        .Returns((d.ToViewModel(DiagnosticLevel.Warning), null));
                    mock.Setup(x => x.CreateGroupNodeViewModel(
                            It.Is<string>(t => string.Equals(t, d.ProviderType, StringComparison.OrdinalIgnoreCase)),
                            DiagnosticLevel.Error))
                        .Returns((d.ToViewModel(DiagnosticLevel.Error), null));
                }
            }

            if (createTargetViewModel is not null)
            {
                foreach (var d in createTargetViewModel)
                {
                    mock.Setup(x => x.CreateTargetViewModel(
                            It.Is<TargetFramework>(t => string.Equals(t.TargetFrameworkAlias, d.Caption, StringComparison.OrdinalIgnoreCase)),
                            DiagnosticLevel.None))
                        .Returns(d.ToViewModel(DiagnosticLevel.None));
                    mock.Setup(x => x.CreateTargetViewModel(
                            It.Is<TargetFramework>(t => string.Equals(t.TargetFrameworkAlias, d.Caption, StringComparison.OrdinalIgnoreCase)),
                            DiagnosticLevel.Warning))
                        .Returns(d.ToViewModel(DiagnosticLevel.Warning));
                    mock.Setup(x => x.CreateTargetViewModel(
                            It.Is<TargetFramework>(t => string.Equals(t.TargetFrameworkAlias, d.Caption, StringComparison.OrdinalIgnoreCase)),
                            DiagnosticLevel.Error))
                        .Returns(d.ToViewModel(DiagnosticLevel.Error));
                }
            }

            return mock.Object;
        }
    }
}
